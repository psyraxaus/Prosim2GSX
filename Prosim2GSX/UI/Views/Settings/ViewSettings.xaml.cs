using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppLogger;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Prosim2GSX.UI.Views.Settings
{
    public partial class ViewSettings : UserControl, IView
    {
        protected virtual ModelSettings ViewModel { get; }
        protected virtual ViewModelSelector<KeyValuePair<string, double>, string> ViewModelSelector { get; }

        public ViewSettings()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;

            ViewModel.BindStringNumber(nameof(ViewModel.ProsimWeightBag), InputBagWeight, "15", new ValidationRuleRange<double>(1, 15));
            ViewModel.BindStringNumber(nameof(ViewModel.FuelResetDefaultKg), InputFuelDefault, "3000", new ValidationRuleRange<double>(1000, 6000));
            ViewModel.BindStringNumber(nameof(ViewModel.FuelCompareVariance), InputFuelVariance, "25", new ValidationRuleRange<double>(10, 100));
            ViewModel.BindStringInteger(nameof(ViewModel.CargoPercentChangePerSec), InputCargoRate, "5", new ValidationRuleRange<int>(1, 25));
            ViewModel.BindStringInteger(nameof(ViewModel.DoorCargoDelay), InputDoorCargoCloseDelay, "16", new ValidationRuleRange<int>(1, 180));
            ViewModel.BindStringInteger(nameof(ViewModel.DoorCargoOpenDelay), InputDoorCargoOpenDelay, "2", new ValidationRuleRange<int>(1, 90));
            // Not used in Prosim
            //ViewModel.BindStringInteger(nameof(ViewModel.RefuelPanelOpenDelay), InputRefuelOpenDelay, "10", new ValidationRuleRange<int>(1, 90));
            //ViewModel.BindStringInteger(nameof(ViewModel.RefuelPanelCloseDelay), InputRefuelCloseDelay, "42", new ValidationRuleRange<int>(1, 180));
            ViewModel.BindStringInteger(nameof(ViewModel.GsxMenuStartupMaxFail), InputGsxMaxFail, "4", new ValidationRuleRange<int>(1,16));

            ViewModelSelector = new(ListSavedFuel, ViewModel.ModelSavedFuel);
            ViewModelSelector.BindRemoveButton(ButtonRemove);
        }

        public virtual void Start()
        {

        }

        public virtual void Stop()
        {

        }

        private void ButtonBrowseProSimSdk_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select ProSimSDK.dll",
                Filter = "ProSim SDK (ProSimSDK.dll)|ProSimSDK.dll|DLL files (*.dll)|*.dll|All files (*.*)|*.*",
                DefaultExt = ".dll",
                CheckFileExists = true,
                CheckPathExists = true
            };

            // Set initial directory from current path if valid
            string currentPath = ViewModel.ProSimSdkPath;
            if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(currentPath);
            }

            if (dialog.ShowDialog() != true)
                return;

            string selectedPath = dialog.FileName;

            // Validate the selected file
            string fileName = Path.GetFileName(selectedPath);
            if (!fileName.Equals("ProSimSDK.dll", StringComparison.OrdinalIgnoreCase))
            {
                var result = MessageBox.Show(
                    $"The selected file '{fileName}' may not be the correct ProSim SDK file.\nExpected 'ProSimSDK.dll'.\n\nDo you want to use this file anyway?",
                    "ProSim SDK",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            // Update the path
            string previousPath = ViewModel.ProSimSdkPath;
            ViewModel.ProSimSdkPath = selectedPath;
            InputProSimSdkPath.Text = selectedPath;

            Logger.Information($"ProSim SDK path changed via Settings: {selectedPath}");

            // Show restart required notification
            MessageBox.Show(
                "ProSim SDK path has been updated.\n\nPlease restart the application for the change to take effect.",
                "Restart Required",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
