using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Win32;

namespace JPPInstaller
{
    public abstract class HostInstall : INotifyPropertyChanged
    {
        public string Name { get; set; }
        
        public string RegKey { get; set; }
        
        public bool HostInstalled { get; set; }
        
        public List<ReleaseStream> Streams { get; set; }

        public List<string> Locales { get; set; }

        public string HostVerison { get; set; }
        
        public bool Busy
        {
            get { return _busy; }
            set
            {
                _busy = value;
                NotifyPropertyChanged();
            }
        }

        private bool _busy;
        
        public ReleaseStream Active
        {
            get { return _active; }
            set
            {
                _active = value;
                StreamChanged();
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(StreamInstalled));
            }
        }

        internal void AddLocales(List<string> locales)
        {
            Locales = locales;
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

        protected ReleaseStream _active;

        public string ProductFamily { get; set; }

        public bool Deprecated { get; set; }

        public HostInstall(string name, string regKey, bool deprecated = false)
        {
            Name = name;
            HostVerison = name.Split(' ')[1];
            RegKey = regKey;
            Streams = new List<ReleaseStream>();
            Deprecated = deprecated;            
        }

        /// <summary>
        /// Scan the registry for an expected key indicating the host software is present and installed
        /// </summary>
        public void CheckRegistryForHostInstall()
        {
            foreach (string locale in Locales)
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey($"SOFTWARE\\{RegKey}{locale}"))
                {
                    HostInstalled = CheckProductInstalled(key);
                    if(HostInstalled)
                        break;
                }
            }
        }

        protected virtual bool CheckProductInstalled(RegistryKey key)
        {
            return key != null;
        }

        internal abstract Task RemoveActive();        
        

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

        protected abstract void CheckForUpdate();

        protected abstract bool CheckRegistryForStream(string stream);

        private bool CheckStreamExists()
        {
            return false;
        }

        protected abstract Task StreamChanged();

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

            /*BlobContainerClient client =
                new BlobContainerClient(new Uri($"https://jppcdnstorage.blob.core.windows.net/{ProductFamily}"));*/
            string path = $"https://jppcdnstorage.blob.core.windows.net/{ProductFamily}";

            BlobContainerClient client = new BlobContainerClient(new Uri(path)); 

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

        protected abstract Task DownloadActive();
        

        protected abstract string BuildPath();

        protected abstract string GetVersionFromPath(string path);

        public abstract void SetActive();

        public abstract Task UpdateActive();

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
