using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
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
            // Get installed version
            string installedVersion = await GetInstalledVersion() ?? "";
            Debug.WriteLine($"Currently installed version: {installedVersion}");

            string latestVersion = "0.0.0";
            string changelog = "...";

            // Set up tasks for getting current version & changelog
            var tasks = new Task[] {
                Task.Run(async () =>
                {
                    // Get latest game version
                    latestVersion = await TibboAPI.GetCurrentVersion() ?? "0.0.0";

                    // Update view-model data
                    _viewModel.Version = latestVersion;
                }),
                Task.Run(async () =>
                {
                    // Get game changelog
                    changelog = await TibboAPI.GetChangelog() ?? "...";

                    // Update view-model data
                    _viewModel.Changelog = changelog;
                }),
            };
            // Wait for tasks to finish
            await Task.WhenAll(tasks);

            // Check if installedVersion is different from latestVersion
            // if true, download and install new files
            if (installedVersion != latestVersion)
            {
                // Download and install latest game binaries
                if(!await Launcher.DownloadAndInstall(updateVersionFile: true))
                {
                    // Failed!
                    Debug.WriteLine($"Failed installing latest game binaries!!");
                }
                else
                {
                    // Success
                    Debug.WriteLine($"Successfully installed latest game binaries!");

                    // Get installed version after update
                    installedVersion = await GetInstalledVersion() ?? "";
                    Debug.WriteLine($"Update succesful! currently installed version: {installedVersion}");
                }
            }

            // Launch game!
            Process.Start("Intersect Client");
            Application.Current.Shutdown();
        }
    
        /// <summary>
        /// Get currently installed version string
        /// </summary>
        /// <returns>Installed client version</returns>
        private async Task<string?> GetInstalledVersion()
        {
            // Check for "version" file
            if (!File.Exists("version"))
                return null; // No version file, return null

            try
            {
                // Read version from file
                string version = (await File.ReadAllTextAsync("version"))
                    .Trim();

                // Return version
                return version;
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Encountered an error while checking the installed Tibbo version. Error: {ex}");
            }

            return null;
        }
    }

    /// <summary>
    /// Tibbo Launcher utilities
    /// </summary>
    public static class Launcher
    {
        // TODO: Add progress reporting so that we can update UI progressbar...
        /// <summary>
        /// Download and install game binaries
        /// </summary>
        public static async Task<bool> DownloadAndInstall(bool updateVersionFile = true)
        {
            try
            {
                // Temp file name
                string filename = "tibbo-tmp.zip";

                // Open file handle
                using (var filestream = File.Open(filename, FileMode.Create, FileAccess.ReadWrite))
                {
                    // Get download stream
                    using (var downloadStream =  await TibboAPI.GetDownloadStream()) 
                    {
                        // Return if downloadStream is null
                        if (downloadStream == null)
                            return false; // Failed

                        // Write to filestream until download stream is empty
                        const int CHUNK_SIZE = 1024*1024*8; // 8MB at a time
                        byte[] chunk = new byte[CHUNK_SIZE];
                        do
                        {
                            // Read from download stream
                            int numRead = await downloadStream.ReadAsync(chunk, 0, CHUNK_SIZE);
                            // Break if numRead is zero
                            if (numRead == 0)
                                break; // End of stream

                            // Write numRead to filestream
                            await filestream.WriteAsync(chunk, 0, numRead);
                        }
                        while (true);
                    }
                }

                // Finished downloading game zip binaries
                // Extract to directory, overwrite existing
                ZipFile.ExtractToDirectory(filename, ".", true);

                // Delete temp file
                File.Delete(filename);

                // Update version file!
                if (updateVersionFile)
                {
                    // Version file
                    string versionFilename = "version";

                    // Get latest version
                    string? latestVersion = await TibboAPI.GetCurrentVersion();

                    // Should we fail the update if we can't get the version...?
                    if (latestVersion == null)
                        return false;

                    // Write to "version" file!
                    File.WriteAllText(versionFilename, latestVersion.Trim());
                }

                // Successfully downloaded and installed
                return true;
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Encountered an error while downloading Tibbo game binaries. Error: {ex}");
            }

            // Download and install failed
            return false;
        }
    }

    /// <summary>
    /// Easy-to-use Tibbo API wrapper
    /// </summary>
    public static class TibboAPI
    {
        private static class Endpoints
        {
            public static Uri Base => new Uri("https://raw.githubusercontent.com/knut-egil/tibbo-client/main/");

            #region API endpoints
            public static Uri API => new Uri(Base,"api/");
            public static Uri Version => new Uri(API, "version");
            public static Uri Changelog => new Uri(API, "changelog");
            #endregion

            #region CDN 
            public static Uri Download => new Uri("https://drive.usercontent.google.com/download?id=1dvBH1b-w87WthFHBjU2UZy0F356dm6MM&export=download&authuser=3&confirm=t&uuid=303f65ab-70ac-4681-aa56-9d1a4af66f4a&at=APZUnTVDfmFAmu7M5HKqE3URdQbj%3A1706467232895");
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
                    var result = await client.GetStreamAsync(Endpoints.Download);

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
