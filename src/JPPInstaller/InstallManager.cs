using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace JPPInstaller
{
    class InstallManager
    {
        Dictionary<string, ReleaseStream> Streams { get; set; }
        private List<string> branches;

        private List<string> locales;

        public ObservableCollection<HostInstall> Hosts { get; set; }

        public InstallManager()
        {
            Streams = new Dictionary<string, ReleaseStream>();
            Hosts = new ObservableCollection<HostInstall>();
        }

        public void AddReleaseStream(ReleaseStream stream)
        {
            Streams.Add(stream.Name, stream);
        }

        internal void AddBranchNames(List<string> lists)
        {
            branches = lists;
        }

        internal void AddLocales(List<string> supportedLocales)
        {
            locales = supportedLocales;
        }

        public async Task AddHost(HostInstall host)
        {
            host.AddLocales(locales);
            Hosts.Add(host);            
            await host.AddStreams(Streams.Values);
            await host.AddBranches(branches);
            host.CheckRegistryForHostInstall();
        }
    }
}
