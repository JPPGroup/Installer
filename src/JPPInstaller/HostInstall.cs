using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Win32;

namespace JPPInstaller
{
    public class HostInstall
    {
        public string Name { get; set; }
        
        public string RegKey { get; set; }
        
        public bool HostInstalled { get; set; }
        
        public List<ReleaseStream> Streams { get; set; }

        public string HostVerison { get; set; }
        
        public ReleaseStream Active
        {
            get { return _active; }
            set
            {
                _active = value;
                StreamChanged();
            }
        }

        public bool StreamInstalled
        {
            get
            {
                return _active != null;
            }
        }

        public bool UpdateAvailable
        {
            get;
            set;
        }

        private ReleaseStream _active;

        public HostInstall(string name, string regKey)
        {
            Name = name;
            HostVerison = name.Split(' ')[1];
            RegKey = regKey;
            Streams = new List<ReleaseStream>();
            
            CheckRegistryForHostInstall();
        }

        /// <summary>
        /// Scan the registry for an expected key indicating the host software is present and installed
        /// </summary>
        private void CheckRegistryForHostInstall()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey($"SOFTWARE\\{RegKey}"))
            {
                HostInstalled = key != null;
            }
        }

        internal void RemoveActive()
        {
            using (RegistryKey key =
                Registry.CurrentUser.OpenSubKey($"SOFTWARE\\{RegKey}\\Applications", true))
            {
                key.DeleteSubKeyTree("Ironstone");
            }

            _active = null;
        }

        /// <summary>
        /// Add collection of streams to the application and determine if any are active
        /// </summary>
        /// <param name="values">Release streams to add</param>
        internal async Task AddStreams(IEnumerable<ReleaseStream> values)
        {
            foreach (ReleaseStream releaseStream in values)
            {
                //Streams.Add(releaseStream);
                await AddReleaseIfExists(releaseStream.Name);
            }
        }

        private void CheckForUpdate()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\{RegKey}\\Applications\\Ironstone"))
            {
                if (key == null)
                {
                    UpdateAvailable = false;
                    return;
                }

                object value = key.GetValue("LOADER");
                if (value == null)
                {
                    UpdateAvailable = false;
                    return;
                }

                string currentVersion = GetVersionFromPath((string) value);

                long currentId = long.Parse(currentVersion);
                if (currentId < _active.ReleaseId)
                {
                    UpdateAvailable = true;
                }
                else
                {
                    UpdateAvailable = false;
                }
            }
        }

        private bool CheckRegistryForStream(string stream)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\{RegKey}\\Applications\\Ironstone"))
            {
                if (key == null)
                    return false;

                object value = key.GetValue("ReleaseStream");
                if (value == null)
                    return false;

                string regName = (string)value;
                if (regName == stream)
                    return true;
            }

            return false;
        }

        private bool CheckStreamExists()
        {
            return false;
        }
        
        private void StreamChanged()
        {
            Task.Run(async () => { 
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\{RegKey}\\Applications\\Ironstone"))
            {
                if (key == null)
                {
                    Registry.CurrentUser.CreateSubKey($"SOFTWARE\\{RegKey}\\Applications\\Ironstone");
                }
            }

            string loadPath = BuildPath();
            if (!File.Exists(loadPath))
            {
                await DownloadActive();
            }

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\{RegKey}\\Applications\\Ironstone", true))
            {
                key.SetValue("ReleaseStream", _active.Name);
                key.SetValue("LOADCTRLS", 2, RegistryValueKind.DWord);
                key.SetValue("MANAGED", 1, RegistryValueKind.DWord);
                key.SetValue("LOADER", loadPath, RegistryValueKind.String);
                key.SetValue("DESCRIPTION", "JPP Ironstone", RegistryValueKind.String);
            }
            });
        }

        internal async Task AddBranches(List<string> branches)
        {
            foreach (string branch in branches)
            {
                await AddReleaseIfExists(branch);
            }
        }
        
        public async Task AddReleaseIfExists(string branchName)
        {
            string remoteName = branchName;
            
            //Handle "special names"
            switch (branchName)
            {
                case "Nightly":
                    remoteName = "master";
                    break;

                default:
                    break;
            }
            
            string fullpath = $"refs/heads/{remoteName}";

            BlobContainerClient client =
                new BlobContainerClient(new Uri("https://jppcdnstorage.blob.core.windows.net/ironstone"));

            var result = client.GetBlobsAsync(prefix: $"{fullpath}/{HostVerison}");

            long buildId = 0;
            ReleaseStream rs = new ReleaseStream()
            {
                Name = branchName,
                Class = ReleaseClass.Alpha
            };
            
            await foreach (BlobItem blob in result)
            {
                if (blob.Name.EndsWith(".zip") && !blob.Deleted)
                {
                    string blobId = Path.GetFileNameWithoutExtension(blob.Name);
                    long foundId = long.Parse(blobId);

                    if (foundId > buildId)
                    {
                        buildId = foundId;
                        rs.BaseUrl = new Uri(client.Uri.AbsoluteUri + "/" + blob.Name);
                        rs.ReleaseId = buildId;
                    }
                }
            }

            if (buildId > 0)
            {
                Streams.Add(rs);
            }
        }

        private async Task DownloadActive()
        {
            try
            {
                string dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), $"JPP\\Ironstone\\{Name}");
                string destZip = Path.Combine(dest, $"{_active.ReleaseId}.zip");
                string sourceUrl = $"{_active.BaseUrl}";

                Directory.CreateDirectory(dest);
                
                WebClient client = new WebClient();

                await client.DownloadFileTaskAsync(sourceUrl, destZip);
                ZipFile.ExtractToDirectory(destZip, $"{dest}\\{_active.ReleaseId}" );
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }

        private string BuildPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), $"JPP\\Ironstone\\{Name}\\{_active.ReleaseId}\\IronstoneCore.dll");
        }

        private string GetVersionFromPath(string path)
        {
            //return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), $"JPP\\Ironstone\\{Name}\\{_active.ReleaseId}\\IronstoneCore.dll");
            int location = path.IndexOf(Name) + Name.Length + 1;
            string remainder = path.Substring(location);
            string[] parts = remainder.Split("\\");
            return parts[0];

            throw new InvalidOperationException();
        }

        public void SetActive()
        {
            string name;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\{RegKey}\\Applications\\Ironstone", false))
            {
                if (key == null)
                    return;
                
                name = (string)key.GetValue("ReleaseStream");
            }

            foreach (ReleaseStream releaseStream in Streams)
            {
                if (releaseStream.Name.Equals(name))
                {
                    _active = releaseStream;
                    CheckForUpdate();
                }
            }
        }

        public void UpdateActive()
        {
            Task.Run(async () => {
                
                string loadPath = BuildPath();
                if (!File.Exists(loadPath))
                {
                    await DownloadActive();
                }

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\{RegKey}\\Applications\\Ironstone", true))
                {
                    key.SetValue("ReleaseStream", _active.Name);
                    key.SetValue("LOADCTRLS", 2, RegistryValueKind.DWord);
                    key.SetValue("MANAGED", 1, RegistryValueKind.DWord);
                    key.SetValue("LOADER", loadPath, RegistryValueKind.String);
                    key.SetValue("DESCRIPTION", "JPP Ironstone", RegistryValueKind.String);
                }
            });
        }
    }
}
