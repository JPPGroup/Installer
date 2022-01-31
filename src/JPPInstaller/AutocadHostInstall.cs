using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace JPPInstaller
{
    public class AutocadHostInstall : HostInstall
    {       

        public AutocadHostInstall(string name, string regKey, bool deprecated = false) : base(name, regKey, deprecated)
        {
            ProductFamily = "ironstone";
        }

        internal override async Task RemoveActive()
        {
            using (RegistryKey key =
                Registry.CurrentUser.OpenSubKey($"SOFTWARE\\{RegKey}\\Applications", true))
            {
                key.DeleteSubKeyTree("Ironstone");
            }

            Active = null;
        }
                

        protected override void CheckForUpdate()
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

        protected override bool CheckRegistryForStream(string stream)
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
        
        protected override async Task StreamChanged()
        {
            if (Active == null)
                return;

            Busy = true;

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

            Busy = false;
        }

        protected override async Task DownloadActive()
        {
            try
            {
                string dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), $"JPP\\Ironstone\\{Name}");
                string destZip = Path.Combine(dest, $"{_active.ReleaseId}.zip");
                string sourceUrl = $"{_active.BaseUrl}";

                Directory.CreateDirectory(dest);

                WebClient client = new WebClient();

                await client.DownloadFileTaskAsync(sourceUrl, destZip);
                ZipFile.ExtractToDirectory(destZip, $"{dest}\\{_active.ReleaseId}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        protected override string BuildPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), $"JPP\\Ironstone\\{Name}\\{_active.ReleaseId}\\IronstoneCore.dll");
        }

        protected override string GetVersionFromPath(string path)
        {
            //return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), $"JPP\\Ironstone\\{Name}\\{_active.ReleaseId}\\IronstoneCore.dll");
            int location = path.IndexOf(Name) + Name.Length + 1;
            string remainder = path.Substring(location);
            string[] parts = remainder.Split("\\");
            return parts[0];

            throw new InvalidOperationException();
        }

        public override void SetActive()
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

        public override async Task UpdateActive()
        {
            Busy = true;

            string loadPath = BuildPath();
            if (!File.Exists(loadPath))
            {
                await DownloadActive();
            }

            using (RegistryKey key =
                Registry.CurrentUser.OpenSubKey($"SOFTWARE\\{RegKey}\\Applications\\Ironstone", true))
            {
                key.SetValue("ReleaseStream", _active.Name);
                key.SetValue("LOADCTRLS", 2, RegistryValueKind.DWord);
                key.SetValue("MANAGED", 1, RegistryValueKind.DWord);
                key.SetValue("LOADER", loadPath, RegistryValueKind.String);
                key.SetValue("DESCRIPTION", "JPP Ironstone", RegistryValueKind.String);
            }

            Busy = false;
        }
    }
}
