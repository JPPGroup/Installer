using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Windows;
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

        private ReleaseStream _active;

        public HostInstall(string name, string regKey)
        {
            Name = name;
            RegKey = regKey;
            Streams = new List<ReleaseStream>();
            
            CheckRegistry();
        }

        private void CheckRegistry()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey($"SOFTWARE\\{RegKey}"))
            {
                HostInstalled = key != null;
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

                string regName = (string) value;
                if (regName == stream)
                    return true;

            }
            
            return false;
        }

        internal void AddStreams(IEnumerable<ReleaseStream> values)
        {
            foreach (ReleaseStream releaseStream in values)
            {
                Streams.Add(releaseStream);
                if (CheckRegistryForStream(releaseStream.Name))
                    _active = releaseStream;
            }
        }

        private bool CheckStreamExists()
        {
            return false;
        }
        
        private void StreamChanged()
        {
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
                DownloadActive();
            }

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\{RegKey}\\Applications\\Ironstone", true))
            {
                key.SetValue("ReleaseStream", _active.Name);
                key.SetValue("LOADCTRLS", 2, RegistryValueKind.DWord);
                key.SetValue("MANAGED", 1, RegistryValueKind.DWord);
                key.SetValue("LOADER", loadPath, RegistryValueKind.String);
                key.SetValue("DESCRIPTION", "JPP Ironstone", RegistryValueKind.String);
            }
        }

        private void DownloadActive()
        {
            string dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"\\JPP\\Ironstone\\{Name}");
            string destZip = Path.Combine(dest, $"{_active.ReleaseId}.zip");
            string sourceUrl = $"{_active.BaseUrl}/{HostVerison}/{_active.ReleaseId}.zip";

            WebClient client = new WebClient();

            client.DownloadFileTaskAsync(sourceUrl, dest);
            ZipFile.ExtractToDirectory(destZip, $"{dest}\\{_active.ReleaseId}" );
        }

        private string BuildPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"\\JPP\\Ironstone\\{Name}\\{_active.ReleaseId}\\IronstoneCore.dll");
        }
    }
}
