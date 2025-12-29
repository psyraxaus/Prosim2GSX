using Prosim2GSX.AppConfig;
using Prosim2GSX.Prosim;
using CFIT.AppLogger;
using CFIT.AppTools;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Prosim2GSX.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for ProSimSdkDialog.xaml
    /// </summary>
    public partial class ProSimSdkDialog : Window
    {
        private Config config;
        private ProsimSdkService testSdkService;
        
        public string SelectedPath { get; private set; }
        public bool ConnectionTested { get; private set; }
        public bool ConnectionSuccessful { get; private set; }

        public ProSimSdkDialog(Config config, bool allowCloseWithoutPath = false)
        {
            InitializeComponent();
            this.config = config;
            this.Topmost = true;

            // If there's already a path configured, show it
            if (!string.IsNullOrEmpty(config.ProSimSdkPath))
            {
                PathTextBox.Text = config.ProSimSdkPath;
                ValidatePath(config.ProSimSdkPath);
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select ProSimSDK.dll",
                Filter = "ProSim SDK (ProSimSDK.dll)|ProSimSDK.dll|DLL files (*.dll)|*.dll|All files (*.*)|*.*",
                DefaultExt = ".dll",
                CheckFileExists = true,
                CheckPathExists = true
            };

            // Set initial directory if a path is already configured
            if (!string.IsNullOrEmpty(config.ProSimSdkPath) && File.Exists(config.ProSimSdkPath))
            {
                openFileDialog.InitialDirectory = Path.GetDirectoryName(config.ProSimSdkPath);
            }

            if (openFileDialog.ShowDialog() == true)
            {
                PathTextBox.Text = openFileDialog.FileName;
                ValidatePath(openFileDialog.FileName);
            }
        }

        private void ValidatePath(string path)
        {
            // Reset connection status when path changes
            ConnectionTested = false;
            ConnectionSuccessful = false;
            ConnectionStatusBorder.Visibility = Visibility.Collapsed;

            if (string.IsNullOrEmpty(path))
            {
                OkButton.IsEnabled = false;
                TestConnectionButton.IsEnabled = false;
                StatusText.Visibility = Visibility.Collapsed;
                return;
            }

            if (!File.Exists(path))
            {
                StatusText.Text = "The selected file does not exist.";
                StatusText.Foreground = Brushes.Red;
                StatusText.Visibility = Visibility.Visible;
                OkButton.IsEnabled = false;
                TestConnectionButton.IsEnabled = false;
                return;
            }

            // Check if it's a DLL file
            if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                StatusText.Text = "Please select a valid DLL file.";
                StatusText.Foreground = Brushes.Orange;
                StatusText.Visibility = Visibility.Visible;
                OkButton.IsEnabled = true; // Allow user to proceed if they're sure
                TestConnectionButton.IsEnabled = true;
                return;
            }

            // Check if it's specifically ProSimSDK.dll (warning only)
            string fileName = Path.GetFileName(path);
            if (!fileName.Equals("ProSimSDK.dll", StringComparison.OrdinalIgnoreCase))
            {
                StatusText.Text = $"Warning: Selected file '{fileName}' may not be the correct ProSim SDK file. Expected 'ProSimSDK.dll'.";
                StatusText.Foreground = Brushes.Orange;
                StatusText.Visibility = Visibility.Visible;
            }
            else
            {
                StatusText.Text = "Valid ProSim SDK file selected.";
                StatusText.Foreground = Brushes.Green;
                StatusText.Visibility = Visibility.Visible;
            }

            OkButton.IsEnabled = true;
            TestConnectionButton.IsEnabled = true;
        }

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            string sdkPath = PathTextBox.Text;
            
            if (string.IsNullOrEmpty(sdkPath) || !File.Exists(sdkPath))
            {
                ShowConnectionStatus(false, "Invalid SDK path", "Please select a valid SDK file first.");
                return;
            }

            // Disable button during test
            TestConnectionButton.IsEnabled = false;
            TestConnectionButton.Content = "Testing...";
            
            try
            {
                // Save current path temporarily
                string originalPath = config.ProSimSdkPath;
                config.ProSimSdkPath = sdkPath;

                // Setup assembly resolver for the test
                SetupTestAssemblyResolver(sdkPath);

                // Create and initialize test SDK service
                testSdkService = new ProsimSdkService(config);
                bool initialized = await testSdkService.Initialize();

                if (!initialized)
                {
                    ShowConnectionStatus(false, "SDK Initialization Failed",
                        "Could not initialize the ProSim SDK. Please verify the file is a valid ProSim SDK assembly.");
                    config.ProSimSdkPath = originalPath;
                    return;
                }

                // Test connection
                bool connected = await testSdkService.VerifyConnection();
                
                if (connected)
                {
                    string prosimBinary = config.ProsimBinary;
                    bool binaryRunning = Sys.GetProcessRunning(prosimBinary);
                    
                    ShowConnectionStatus(true, "Connection Successful",
                        $"SDK loaded successfully. ProSim process ({prosimBinary}) is {(binaryRunning ? "running" : "not running")}.");
                    ConnectionSuccessful = true;
                }
                else
                {
                    string prosimBinary = config.ProsimBinary;
                    bool binaryRunning = Sys.GetProcessRunning(prosimBinary);
                    
                    ShowConnectionStatus(false, "Connection Failed",
                        $"SDK loaded but connection verification failed. ProSim process ({prosimBinary}) is {(binaryRunning ? "running" : "not running")}. The connection may succeed once ProSim is running.");
                }

                ConnectionTested = true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                ShowConnectionStatus(false, "Test Failed",
                    $"Error testing SDK connection: {ex.Message}");
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
                TestConnectionButton.Content = "Test Connection";
                
                // Clean up test service
                if (testSdkService != null)
                {
                    await testSdkService.Stop();
                    testSdkService = null;
                }
            }
        }

        private void SetupTestAssemblyResolver(string sdkPath)
        {
            string sdkDirectory = Path.GetDirectoryName(sdkPath);
            
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name.Contains("ProSimSDK") || args.Name.Contains("ProsimInterface"))
                {
                    try
                    {
                        if (File.Exists(sdkPath))
                        {
                            return System.Reflection.Assembly.LoadFrom(sdkPath);
                        }

                        string assemblyName = args.Name.Split(',')[0];
                        string assemblyPath = Path.Combine(sdkDirectory, assemblyName + ".dll");
                        
                        if (File.Exists(assemblyPath))
                        {
                            return System.Reflection.Assembly.LoadFrom(assemblyPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error resolving assembly {args.Name}: {ex.Message}");
                    }
                }
                
                return null;
            };
        }

        private void ShowConnectionStatus(bool success, string title, string details)
        {
            ConnectionStatusBorder.Visibility = Visibility.Visible;
            ConnectionStatusText.Text = title;
            ConnectionDetailsText.Text = details;
            
            if (success)
            {
                ConnectionStatusBorder.BorderBrush = Brushes.Green;
                ConnectionStatusText.Foreground = Brushes.Green;
            }
            else
            {
                ConnectionStatusBorder.BorderBrush = Brushes.Orange;
                ConnectionStatusText.Foreground = Brushes.Orange;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedPath = PathTextBox.Text;

            // Save the path to config
            config.ProSimSdkPath = SelectedPath;
            config.SaveConfiguration();

            // Notify UI that the property has changed
            config.NotifyPropertyChanged(nameof(config.ProSimSdkPath));

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}