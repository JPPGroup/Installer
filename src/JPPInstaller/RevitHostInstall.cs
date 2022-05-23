using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace JPPInstaller
{
    public class RevitHostInstall : HostInstall
    {       

        public RevitHostInstall(string name, string regKey, bool deprecated = false, bool experimental = false) : base(name, regKey, deprecated, experimental)
        {
            ProductFamily = "cedar";
        }

        internal override async Task RemoveActive()
        {
            using (RegistryKey key =
                Registry.CurrentUser.OpenSubKey($"SOFTWARE\\JPP Consulting\\Cedar", true))
            {
                key.DeleteSubKeyTree($"{ HostVerison}");
            }

            File.Delete($"C:\\ProgramData\\Autodesk\\Revit\\Addins\\{HostVerison}\\Cedar.addin");
            Directory.Delete($"C:\\ProgramData\\Autodesk\\Revit\\Addins\\{HostVerison}\\Cedar", true);

            Active = null;
        }

        protected override bool CheckProductInstalled(RegistryKey key)
        {
            if (!base.CheckProductInstalled(key))
                return false;


            return key.GetValue("InstallationLocation") != null;
        }


        protected override void CheckForUpdate()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\JPP Consulting\\Cedar"))
            {
                if (key == null)
                {
                    UpdateAvailable = false;
                    return;
                }

                object value = key.GetValue("VERSION");
                if (value == null)
                {
                    UpdateAvailable = false;
                    return;
                }

                string currentVersion = ((string) value);

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
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\JPP Consulting\\Cedar\\{HostVerison}"))
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

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\JPP Consulting\\Cedar\\{HostVerison}"))
            {
                if (key == null)
                {
                    Registry.CurrentUser.CreateSubKey($"SOFTWARE\\JPP Consulting\\Cedar\\{HostVerison}");
                }
            }

            await DownloadActive();

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\JPP Consulting\\Cedar\\{HostVerison}", true))
            {
                key.SetValue("ReleaseStream", _active.Name);
                key.SetValue("VERSION", _active.ReleaseId);
            }

            Busy = false;
        }

        protected override async Task DownloadActive()
        {
            try
            {
                string dest = $"C:\\ProgramData\\Autodesk\\Revit\\Addins\\{HostVerison}";
                string destZip = Path.Combine(dest, $"{_active.ReleaseId}.zip");
                string destAddin = Path.Combine(dest, $"Cedar");
                string sourceUrl = $"{_active.BaseUrl}";

                if (Directory.Exists(destAddin))
                    Directory.Delete(destAddin);

                if(File.Exists($"{destAddin}.addin"))
                    File.Delete($"{destAddin}.addin");

                Directory.CreateDirectory(dest);

                WebClient client = new WebClient();

                await client.DownloadFileTaskAsync(sourceUrl, destZip);
                ZipFile.ExtractToDirectory(destZip, $"{dest}");

                File.Delete(destZip);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        protected override string BuildPath()
        {
            throw new NotImplementedException();
        }

        protected override string GetVersionFromPath(string path)
        {
            throw new NotImplementedException();
        }

        public override void SetActive()
        {
            string name;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\JPP Consulting\\Cedar\\{HostVerison}", false))
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
                        
            await DownloadActive();            

            using (RegistryKey key =
                Registry.CurrentUser.OpenSubKey($"SOFTWARE\\JPP Consulting\\Cedar\\{HostVerison}", true))
            {
                key.SetValue("ReleaseStream", _active.Name);
                key.SetValue("VERSION", _active.ReleaseId);
            }

            Busy = false;
        }
    }
}
