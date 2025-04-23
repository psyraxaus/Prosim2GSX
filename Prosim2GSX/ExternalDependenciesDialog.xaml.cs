using Microsoft.Win32;
using Prosim2GSX.Models;
using System;
using System.IO;
using System.Windows;

namespace Prosim2GSX
{
    /// <summary>
    /// Interaction logic for ExternalDependenciesDialog.xaml
    /// </summary>
    public partial class ExternalDependenciesDialog : Window
    {
        private readonly ServiceModel _model;

        /// <summary>
        /// Creates a new instance of the ExternalDependenciesDialog
        /// </summary>
        /// <param name="model">The service model to use</param>
        public ExternalDependenciesDialog(ServiceModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            InitializeComponent();

            // Determine which DLLs are missing
            bool prosimMissing = !File.Exists(Path.Combine(App.AppDir, "lib", "ProSimSDK.dll")) &&
                                string.IsNullOrEmpty(_model.ProsimSDKPath);

            bool voicemeeterMissing = !File.Exists(Path.Combine(App.AppDir, "VoicemeeterRemote64.dll")) &&
                                    string.IsNullOrEmpty(_model.VoicemeeterDllPath);

            // Update the information text based on what's missing
            if (prosimMissing && voicemeeterMissing)
            {
                txtInfoMessage.Text = "ProSimSDK.dll and VoicemeeterRemote64.dll were not found in the expected locations. " +
                    "Please specify the paths to these files to ensure Prosim integration and audio control work correctly.";
            }
            else if (prosimMissing)
            {
                txtInfoMessage.Text = "ProSimSDK.dll was not found in the expected location. " +
                    "Please specify the path to this file to ensure Prosim integration works correctly.";
            }
            else if (voicemeeterMissing)
            {
                txtInfoMessage.Text = "VoicemeeterRemote64.dll was not found in the expected location. " +
                    "Please specify the path to this file to ensure audio control via Voicemeeter works correctly.";
            }

            // Pre-populate fields
            txtProsimSDKPath.Text = _model.ProsimSDKPath;
            txtVoicemeeterPath.Text = _model.VoicemeeterDllPath;

            // Auto-detect paths
            if (string.IsNullOrEmpty(_model.ProsimSDKPath) && prosimMissing)
            {
                AttemptAutoDetectProsimPath();
            }

            if (string.IsNullOrEmpty(_model.VoicemeeterDllPath) && voicemeeterMissing)
            {
                AttemptAutoDetectVoicemeeterPath();
            }
        }

        /// <summary>
        /// Handle save button click
        /// </summary>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate paths
            if (!ValidatePaths())
                return;

            // Save settings to disk
            _model.SetSetting("prosimSDKPath", txtProsimSDKPath.Text);
            _model.SetSetting("voicemeeterDllPath", txtVoicemeeterPath.Text);
            _model.SetSetting("externalDependenciesConfigured", "true");

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Validate the entered paths
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        private bool ValidatePaths()
        {
            // Check if ProsimSDK path is valid if specified
            if (!string.IsNullOrEmpty(txtProsimSDKPath.Text) && !File.Exists(txtProsimSDKPath.Text))
            {
                MessageBox.Show("The specified ProsimSDK.dll file does not exist.", "Invalid Path",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Check if Voicemeeter path is valid if specified
            if (!string.IsNullOrEmpty(txtVoicemeeterPath.Text) && !File.Exists(txtVoicemeeterPath.Text))
            {
                MessageBox.Show("The specified VoicemeeterRemote64.dll file does not exist.", "Invalid Path",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Handle browse button click for Prosim SDK path
        /// </summary>
        private void BtnBrowseProsim_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Prosim SDK Library|ProSimSDK.dll|All Files|*.*",
                Title = "Select the ProSimSDK.dll file",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                txtProsimSDKPath.Text = dialog.FileName;
            }
        }

        /// <summary>
        /// Handle browse button click for Voicemeeter path
        /// </summary>
        private void BtnBrowseVoicemeeter_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Voicemeeter Remote Library|VoicemeeterRemote64.dll|All Files|*.*",
                Title = "Select the VoicemeeterRemote64.dll file",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                txtVoicemeeterPath.Text = dialog.FileName;
            }
        }

        /// <summary>
        /// Attempt to auto-detect the Prosim SDK path
        /// </summary>
        private void AttemptAutoDetectProsimPath()
        {
            // Common installation locations for Prosim
            string[] possiblePaths = {
                @"C:\Program Files\ProSim-AR\ProSim737\ProSimSDK.dll",
                @"C:\Program Files (x86)\ProSim-AR\ProSim737\ProSimSDK.dll",
                @"D:\Program Files\ProSim-AR\ProSim737\ProSimSDK.dll",
                @"D:\Program Files (x86)\ProSim-AR\ProSim737\ProSimSDK.dll"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    txtProsimSDKPath.Text = path;
                    return;
                }
            }

            // Registry-based detection
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\ProSim-AR\ProSim737"))
                {
                    if (key != null)
                    {
                        string installPath = key.GetValue("InstallDir") as string;
                        if (!string.IsNullOrEmpty(installPath))
                        {
                            string dllPath = Path.Combine(installPath, "ProSimSDK.dll");
                            if (File.Exists(dllPath))
                            {
                                txtProsimSDKPath.Text = dllPath;
                                return;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore registry errors
            }
        }

        /// <summary>
        /// Attempt to auto-detect the Voicemeeter path
        /// </summary>
        private void AttemptAutoDetectVoicemeeterPath()
        {
            // Common installation locations for Voicemeeter
            string[] possiblePaths = {
                @"C:\Program Files\VB\Voicemeeter\VoicemeeterRemote64.dll",
                @"C:\Program Files (x86)\VB\Voicemeeter\VoicemeeterRemote64.dll",
                @"D:\Program Files\VB\Voicemeeter\VoicemeeterRemote64.dll",
                @"D:\Program Files (x86)\VB\Voicemeeter\VoicemeeterRemote64.dll"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    txtVoicemeeterPath.Text = path;
                    return;
                }
            }

            // Registry-based detection
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\VB\VoiceMeeter"))
                {
                    if (key != null)
                    {
                        string installPath = key.GetValue("UninstallPath") as string;
                        if (!string.IsNullOrEmpty(installPath))
                        {
                            // Strip off the uninstaller exe name
                            installPath = Path.GetDirectoryName(installPath);
                            if (!string.IsNullOrEmpty(installPath))
                            {
                                string dllPath = Path.Combine(installPath, "VoicemeeterRemote64.dll");
                                if (File.Exists(dllPath))
                                {
                                    txtVoicemeeterPath.Text = dllPath;
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore registry errors
            }
        }
    }
}