using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace TibboLauncher
{
    public class LauncherViewModel : INotifyPropertyChanged
    {
        private float _downloadProgress;
        public float DownloadProgress
        {
            get 
            { 
                // Return value
                return _downloadProgress; 
            }
            set
            {
                // Update value
                _downloadProgress = value;

                // Notify property changes
                NotifyPropertyChanged("DownloadProgress");
                NotifyPropertyChanged("StatusText"); // This tells the UI to also update the StatusText when DownloadProgress changes
            }
        }

        public string StatusText
        {
            get
            {
                return $"Downloading... ({DownloadProgress}%)";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LauncherViewModel _viewModel = new LauncherViewModel();
        public MainWindow()
        {
            DataContext = _viewModel;
            InitializeComponent();
        }
    }
}
