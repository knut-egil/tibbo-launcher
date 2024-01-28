﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        #region Progress
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
                NotifyPropertyChanged(); // Notify of "DownloadProgress" change
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
        #endregion

        #region Version
        private string _version = "0.0.0";
        /// <summary>
        /// Raw version string as reported from backend server
        /// </summary>
        public string Version
        {
            get
            {
                return _version;
            }
            set
            {
                _version = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged("VersionText");
            }
        }
        
        /// <summary>
        /// Formatted version text
        /// </summary>
        public string VersionText
        {
            get
            {
                return $"v{Version}";
            }
        }
        #endregion

        #region Changelog
        private string _changelog = "Changelog...";
        /// <summary>
        /// Changelog as reported from backend server
        /// </summary>
        public string Changelog
        {
            get
            {
                return _changelog;
            }
            set
            {
                _changelog = value;
                NotifyPropertyChanged();
            }
        }

        #endregion

        #region INotifyPropertyChanged implement
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
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

            // Call init
            Init();
        }

        /// <summary>
        /// Initialize launcher
        /// </summary>
        private async void Init()
        {
            // Set up tasks for getting current version & changelog
            var tasks = new Task[] {
                Task.Run(async () =>
                {
                    // Get latest game version
                    string latestVersion = await TibboAPI.GetCurrentVersion() ?? "0.0.0";

                    // Update view-model data
                    _viewModel.Version = latestVersion;
                }),
                Task.Run(async () =>
                {
                    // Get game changelog
                    string changelog = await TibboAPI.GetChangelog() ?? "...";

                    // Update view-model data
                    _viewModel.Changelog = changelog;
                }),
            };

            // Wait for tasks to finish
            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// Easy-to-use Tibbo API wrapper
    /// </summary>
    public static class TibboAPI
    {
        private static class Endpoints
        {
            public static Uri Base => new Uri("https://webhook.site/4c8f3912-073a-449b-a0fa-caa8130a0efc/");

            #region API endpoints
            public static Uri API => new Uri(Base, "api/");
            public static Uri Version => new Uri(API, "version");
            public static Uri Changelog => new Uri(API, "changelog");
            #endregion

            #region CDN 
            public static Uri Download => new Uri(Base, "download");
            #endregion
        }

        #region API
        /// <summary>
        /// Get the latest game-client version
        /// </summary>
        /// <returns>Latest game version</returns>
        public static async Task<string?> GetCurrentVersion()
        {
            try
            {
                // Make request to Tibbo current-version endpoint
                using (var client = new HttpClient())
                {
                    // Get result
                    string result = await client.GetStringAsync(Endpoints.Version);

                    // Return
                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Encountered an error while requesting the latest Tibbo game version. Error: {ex}");
            }

            // Return null by default
            return null;
        }

        /// <summary>
        /// Get the version changelog
        /// </summary>
        /// <returns>Version changelog</returns>
        public static async Task<string?> GetChangelog()
        {
            try
            {
                // Make request to Tibbo changelog endpoint
                using (var client = new HttpClient())
                {
                    // Get result
                    string result = await client.GetStringAsync(Endpoints.Changelog);

                    // Return
                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Encountered an error while requesting the Tibbo changelog. Error: {ex}");
            }

            // Return null by default
            return null;
        }
        #endregion

        #region CDN
        /// <summary>
        /// Get stream to download latest game binaries
        /// </summary>
        /// <returns>Game binaries stream</returns>
        public static async Task<Stream?> GetDownloadStream()
        {
            try
            {
                // Make request to Tibbo changelog endpoint
                using (var client = new HttpClient())
                {
                    // Get result
                    var result = await client.GetStreamAsync(Endpoints.Changelog);

                    // Return
                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Encountered an error while requesting the Tibbo game binaries. Error: {ex}");
            }

            // Return null by default
            return null;
        }
        #endregion
    }
}
