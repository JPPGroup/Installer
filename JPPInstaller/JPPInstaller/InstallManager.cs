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

        public async Task AddReleaseIfExists(string branchName)
        {
            string fullpath = $"refs/heads/{branchName}";

            BlobContainerClient client =
                new BlobContainerClient(new Uri("https://jppcdnstorage.blob.core.windows.net/ironstone"));

            var result = client.GetBlobsAsync(prefix: fullpath);

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
                    /*string blobId = blob.Name.Substring(blob.Name.LastIndexOf("/"));
                    blobId = Path.GetFileNameWithoutExtension(blobId);*/
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
            
            Streams.Add(rs.Name, rs);
        }

        public void AddHost(HostInstall host)
        {
            Hosts.Add(host);
            host.AddStreams(Streams.Values);
        }
    }
}
