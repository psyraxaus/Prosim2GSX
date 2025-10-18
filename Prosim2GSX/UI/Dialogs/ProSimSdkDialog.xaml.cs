using Prosim2GSX.AppConfig;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace Prosim2GSX.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for ProSimSdkDialog.xaml
    /// </summary>
    public partial class ProSimSdkDialog : Window
    {
        private Config config;
        
        public string SelectedPath { get; private set; }

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
            if (string.IsNullOrEmpty(path))
            {
                OkButton.IsEnabled = false;
                StatusText.Visibility = Visibility.Collapsed;
                return;
            }

            if (!File.Exists(path))
            {
                StatusText.Text = "The selected file does not exist.";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                StatusText.Visibility = Visibility.Visible;
                OkButton.IsEnabled = false;
                return;
            }

            // Check if it's a DLL file
            if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                StatusText.Text = "Please select a valid DLL file.";
                StatusText.Foreground = System.Windows.Media.Brushes.Orange;
                StatusText.Visibility = Visibility.Visible;
                OkButton.IsEnabled = true; // Allow user to proceed if they're sure
                return;
            }

            // Check if it's specifically ProSimSDK.dll (warning only)
            string fileName = Path.GetFileName(path);
            if (!fileName.Equals("ProSimSDK.dll", StringComparison.OrdinalIgnoreCase))
            {
                StatusText.Text = $"Warning: Selected file '{fileName}' may not be the correct ProSim SDK file. Expected 'ProSimSDK.dll'.";
                StatusText.Foreground = System.Windows.Media.Brushes.Orange;
                StatusText.Visibility = Visibility.Visible;
            }
            else
            {
                StatusText.Text = "Valid ProSim SDK file selected.";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                StatusText.Visibility = Visibility.Visible;
            }

            OkButton.IsEnabled = true;
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