using System;
using System.Collections.Generic;
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

namespace JPPInstaller
{
    /// <summary>
    /// Interaction logic for HostTile.xaml
    /// </summary>
    public partial class HostTile : UserControl
    {
        private HostInstall _model;
        
        public HostTile(HostInstall model)
        {
            InitializeComponent();
            _model = model;
            this.DataContext = model;

            try
            {
                BitmapImage source = new BitmapImage();
                source.BeginInit();
                source.UriSource = new Uri($"pack://application:,,,/Assets/{model.Name}.jpg", UriKind.Absolute);
                source.EndInit();

                if (!model.HostInstalled)
                {

                    FormatConvertedBitmap newFormatedBitmapSource = new FormatConvertedBitmap();
                    newFormatedBitmapSource.BeginInit();
                    newFormatedBitmapSource.Source = source;
                    newFormatedBitmapSource.DestinationFormat = PixelFormats.Gray32Float;
                    newFormatedBitmapSource.EndInit();

                    Banner.Source = newFormatedBitmapSource;
                }
                else
                {
                    Banner.Source = source;
                }

                if(model.Deprecated)
                {
                    DepWarning.Visibility = Visibility.Visible;
                }
                if (model.Experimental)
                {
                    ExperimentalWarning.Visibility = Visibility.Visible;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            this._model.PropertyChanged += (sender, args) =>
            {
                if(args.PropertyName == nameof(HostInstall.Active) && _model.Active != null)
                Xceed.Wpf.Toolkit.MessageBox.Show("Application installed", "", MessageBoxButton.OK, MessageBoxImage.Information);
            };

        }

        private async void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            await _model.RemoveActive();
            Xceed.Wpf.Toolkit.MessageBox.Show("Application removed", "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void UpdateButton_OnClick(object sender, RoutedEventArgs e)
        {
            await _model.UpdateActive();
            Xceed.Wpf.Toolkit.MessageBox.Show("Application updated", "", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
