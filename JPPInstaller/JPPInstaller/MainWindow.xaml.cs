using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
                await GetBranches();

            AddHosts();

                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() => { BuildUI(); }));
            });
        }

        private void BuildUI()
        {
            foreach (HostInstall host in _manager.Hosts)
            {
                Products.Children.Add(new HostTile(host));
            }
        }

        private void AddHosts()
        {
            _manager.AddHost(new HostInstall("Autocad 2021", "Autodesk\\AutoCAD\\R24.0\\ACAD-4101"));
            _manager.AddHost(new HostInstall("Autocad 2020", "Autodesk\\AutoCAD\\R23.1\\ACAD-3001"));
            _manager.AddHost(new HostInstall("Autocad 2019", "Autodesk\\AutoCAD\\R23.0\\ACAD-2001"));
            _manager.AddHost(new HostInstall("Autocad 2018", "Autodesk\\AutoCAD\\R22.0\\ACAD-1001"));
            _manager.AddHost(new HostInstall("Autocad 2017", "Autodesk\\AutoCAD\\R21.0\\ACAD-0001"));
            
            _manager.AddHost(new HostInstall("Civil3D 2018", "Autodesk\\AutoCAD\\R22.0\\ACAD-1000"));
        }

        private void DefaultStreams()
        {
            _manager.AddReleaseStream(new ReleaseStream()
            {
                Name = "Release",
                Class = ReleaseClass.Release
            });
            _manager.AddReleaseStream(new ReleaseStream()
            {
                Name = "Beta",
                Class = ReleaseClass.Beta
            });
            
            _manager.AddReleaseStream(new ReleaseStream()
            {
                Name = "Nightly",
                Class = ReleaseClass.Alpha
            });
        }
        
        private async Task GetBranches()
        {
            var github = new GitHubClient(new ProductHeaderValue("JPPInstaller", "0.1"));
            var branches =  await github.Repository.Branch.GetAll("JPPGroup", "IronstoneMeta");
            foreach (var branch in branches)
            {
                if(branch.Name.StartsWith("features") || branch.Name.StartsWith("bugfixes"))
                    await _manager.AddReleaseIfExists(branch.Name);
            }
        }
    }
}
