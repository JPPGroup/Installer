using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Octokit;

namespace JPPInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private InstallManager _manager;
        
        public MainWindow()
        {
            InitializeComponent();

            _manager = new InstallManager();

            Task.Run(async () => { 
                DefaultStreams();
                _manager.AddBranchNames(await GetBranches());

                await AddHosts();

                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() => { BuildUI(); }));
            });
        }

        private void BuildUI()
        {
            foreach (HostInstall host in _manager.Hosts)
            {
                host.SetActive();
                Products.Children.Add(new HostTile(host));
            }
        }

        private async Task AddHosts()
        {
            await _manager.AddHost(new HostInstall("Autocad 2022", "Autodesk\\AutoCAD\\R24.1\\ACAD-5101"));
            await _manager.AddHost(new HostInstall("Civil3D 2022", "Autodesk\\AutoCAD\\R24.1\\ACAD-5100"));
            await _manager.AddHost(new HostInstall("Autocad 2021", "Autodesk\\AutoCAD\\R24.0\\ACAD-4101"));
            await _manager.AddHost(new HostInstall("Civil3D 2021", "Autodesk\\AutoCAD\\R24.0\\ACAD-4100"));
            await _manager.AddHost(new HostInstall("Autocad 2020", "Autodesk\\AutoCAD\\R23.1\\ACAD-3001"));
            await _manager.AddHost(new HostInstall("Civil3D 2020", "Autodesk\\AutoCAD\\R23.1\\ACAD-3000"));
            await _manager.AddHost(new HostInstall("Autocad 2019", "Autodesk\\AutoCAD\\R23.0\\ACAD-2001"));
            await _manager.AddHost(new HostInstall("Civil3D 2019", "Autodesk\\AutoCAD\\R23.0\\ACAD-2000"));
            //await _manager.AddHost(new HostInstall("Autocad 2018", "Autodesk\\AutoCAD\\R22.0\\ACAD-1001"));
            //await _manager.AddHost(new HostInstall("Autocad 2017", "Autodesk\\AutoCAD\\R21.0\\ACAD-0001"));
            //await _manager.AddHost(new HostInstall("Civil3D 2018", "Autodesk\\AutoCAD\\R22.0\\ACAD-1000"));
        }

        private void DefaultStreams()
        {
            /*_manager.AddReleaseStream(new ReleaseStream()
            {
                Name = "Release",
                Class = ReleaseClass.Release
            });
            _manager.AddReleaseStream(new ReleaseStream()
            {
                Name = "Beta",
                Class = ReleaseClass.Beta
            });*/
            
            _manager.AddReleaseStream(new ReleaseStream()
            {
                Name = "Nightly",
                Class = ReleaseClass.Alpha,
            });
        }
        
        private async Task<List<string>> GetBranches()
        {
            List<string> results = new List<string>();
            var github = new GitHubClient(new ProductHeaderValue("JPPInstaller", "0.1"));
            var branches =  await github.Repository.Branch.GetAll("JPPGroup", "IronstoneMeta");
            foreach (var branch in branches)
            {
                if(branch.Name.StartsWith("features") || branch.Name.StartsWith("bugfixes"))
                    //await _manager.AddReleaseIfExists(branch.Name);
                    results.Add(branch.Name);
            }

            return results;
        }
    }
}
