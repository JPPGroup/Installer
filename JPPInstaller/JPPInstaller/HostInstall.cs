using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace JPPInstaller
{
    public class HostInstall
    {
        public string Name { get; set; }
        
        public string RegKey { get; set; }
        
        public bool HostInstalled { get; set; }
        
        public List<ReleaseStream> Streams { get; set; }
        
        public ReleaseStream Active { get; set; }

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
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey($"SOFTWARE\\{RegKey}\\Applications\\Ironstone"))
            {
                if (key == null)
                    return false;
            }
            
            return false;
        }

        internal void AddStreams(IEnumerable<ReleaseStream> values)
        {
            foreach (ReleaseStream releaseStream in values)
            {
                Streams.Add(releaseStream);
                if (CheckRegistryForStream(releaseStream.Name))
                    Active = releaseStream;
            }
        }

        private bool CheckStreamExists()
        {
            return false;
        }
    }
}
