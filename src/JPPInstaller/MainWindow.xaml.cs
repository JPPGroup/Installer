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
        private InstallManager _manager, _revitManager;
        
        public MainWindow()
        {
            InitializeComponent();

            _manager = new InstallManager();
            _revitManager = new InstallManager();

            Task.Run(async () => { 
                DefaultStreams();
                _manager.AddBranchNames(await GetBranches("IronstoneMeta"));
                _revitManager.AddBranchNames(await GetBranches("Cedar"));

                await AddHosts();

                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() => { BuildUI(); }));
            });
        }

        private void BuildUI()
        {
            foreach (HostInstall host in _manager.Hosts)
            {
                host.SetActive();
                if(!host.Deprecated || host.HostInstalled)
                    Products.Children.Add(new HostTile(host));
            }
            foreach (HostInstall host in _revitManager.Hosts)
            {
                host.SetActive();
                if (!host.Deprecated || host.HostInstalled)
                    RevitProducts.Children.Add(new HostTile(host));
            }
        }

        private async Task AddHosts()
        {
            List<Task> tasks = new List<Task>();
            tasks.Add(_manager.AddHost(new AutocadHostInstall("Autocad 2022", "Autodesk\\AutoCAD\\R24.1\\ACAD-5101")));
            tasks.Add(_manager.AddHost(new AutocadHostInstall("Civil3D 2022", "Autodesk\\AutoCAD\\R24.1\\ACAD-5100")));
            tasks.Add(_manager.AddHost(new AutocadHostInstall("Autocad 2021", "Autodesk\\AutoCAD\\R24.0\\ACAD-4101")));
            tasks.Add(_manager.AddHost(new AutocadHostInstall("Civil3D 2021", "Autodesk\\AutoCAD\\R24.0\\ACAD-4100")));
            tasks.Add(_manager.AddHost(new AutocadHostInstall("Autocad 2020", "Autodesk\\AutoCAD\\R23.1\\ACAD-3001")));
            tasks.Add(_manager.AddHost(new AutocadHostInstall("Civil3D 2020", "Autodesk\\AutoCAD\\R23.1\\ACAD-3000")));
            tasks.Add(_manager.AddHost(new AutocadHostInstall("Autocad 2019", "Autodesk\\AutoCAD\\R23.0\\ACAD-2001", true)));
            tasks.Add(_manager.AddHost(new AutocadHostInstall("Civil3D 2019", "Autodesk\\AutoCAD\\R23.0\\ACAD-2000", true)));
            //await _manager.AddHost(new HostInstall("Autocad 2018", "Autodesk\\AutoCAD\\R22.0\\ACAD-1001"));
            //await _manager.AddHost(new HostInstall("Autocad 2017", "Autodesk\\AutoCAD\\R21.0\\ACAD-0001"));
            //await _manager.AddHost(new HostInstall("Civil3D 2018", "Autodesk\\AutoCAD\\R22.0\\ACAD-1000"));

            tasks.Add(_revitManager.AddHost(new RevitHostInstall("Revit 2022", "Autodesk\\Revit\\2022\\REVIT-05:0809")));
            tasks.Add(_revitManager.AddHost(new RevitHostInstall("Revit 2021", "Autodesk\\Revit\\2021\\REVIT-05:0809")));
            tasks.Add(_revitManager.AddHost(new RevitHostInstall("Revit 2020", "Autodesk\\Revit\\2020\\REVIT-05:0809")));
            tasks.Add(_revitManager.AddHost(new RevitHostInstall("Revit 2019", "Autodesk\\Revit\\2019\\REVIT-05:0809", true)));

            await Task.WhenAll(tasks);
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

            _revitManager.AddReleaseStream(new ReleaseStream()
            {
                Name = "Nightly",
                Class = ReleaseClass.Alpha,
            });
        }
        
        private async Task<List<string>> GetBranches(string repo)
        {
            List<string> results = new List<string>();
            var github = new GitHubClient(new ProductHeaderValue("JPPInstaller", "0.1"));
            var branches =  await github.Repository.Branch.GetAll("JPPGroup", repo);
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
