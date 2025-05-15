﻿﻿﻿using Prosim2GSX.Events;
using Prosim2GSX.Themes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Prosim2GSX.Models;
using Prosim2GSX.Services.Audio;
using Prosim2GSX.Services.GSX.Enums;
using System.Threading;
using static Prosim2GSX.Services.Audio.AudioChannelConfig;
using Prosim2GSX.Services;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;
using Prosim2GSX.ViewModels;


namespace Prosim2GSX
{
    public partial class MainWindow : Window
    {
        protected NotifyIconViewModel notifyModel;
        protected ServiceModel serviceModel;
        protected DispatcherTimer timer;
        private List<SubscriptionToken> _subscriptionTokens = new List<SubscriptionToken>();
        private bool _isLoadingSettings = false;

        private MainViewModel _viewModel;

        public MainWindow(NotifyIconViewModel notifyModel, ServiceModel serviceModel)
        {
            InitializeComponent();

            this.notifyModel = notifyModel;
            this.serviceModel = serviceModel;

            // Create the ViewModel and register it with the locator
            _viewModel = new MainViewModel(serviceModel, notifyModel);
            ViewModelLocator.Instance.RegisterViewModel(_viewModel);

            // Set the DataContext for data binding
            DataContext = _viewModel;

            // Load settings when window is created
            LoadSettings();
        }

        protected void LoadSettings()
        {
            // Set debug verbosity dropdown
            SetDebugVerbosityComboBox();

            if (serviceModel.FlightPlanType == "EFB")
            {
                eFBPlan.IsChecked = true;
                mCDUPlan.IsChecked = false;
            }
            else
            {
                eFBPlan.IsChecked = false;
                mCDUPlan.IsChecked = true;
            }

            chkAutoBoard.IsChecked = serviceModel.AutoBoarding;
            chkAutoConnect.IsChecked = serviceModel.AutoConnect;
            chkJetwayOnly.IsChecked = serviceModel.JetwayOnly;
            chkAutoDeboard.IsChecked = serviceModel.AutoDeboarding;
            chkAutoRefuel.IsChecked = serviceModel.AutoRefuel;
            chkAutoReposition.IsChecked = serviceModel.RepositionPlane;
            chkCallCatering.IsChecked = serviceModel.CallCatering;
            chkConnectPCA.IsChecked = serviceModel.ConnectPCA;
            chkDisableCrewBoarding.IsChecked = serviceModel.DisableCrew;
            chkOpenDoorCatering.IsChecked = serviceModel.SetOpenCateringDoor;
            chkOpenCargoDoors.IsChecked = serviceModel.SetOpenCargoDoors;
            chkPcaOnlyJetway.IsChecked = serviceModel.PcaOnlyJetways;
            chkSynchBypass.IsChecked = serviceModel.SynchBypass;
            chkSaveFuel.IsChecked = serviceModel.SetSaveFuel;
            chkUseAcars.IsChecked = serviceModel.UseAcars;
            chkUseActualPaxValue.IsChecked = serviceModel.UseActualPaxValue;
            chkZeroFuel.IsChecked = serviceModel.SetZeroFuel;

            txtRefuelRate.Text = serviceModel.RefuelRate.ToString(CultureInfo.InvariantCulture);
            txtRepositionDelay.Text = serviceModel.RepositionDelay.ToString(CultureInfo.InvariantCulture);
            txtSimbriefID.Text = serviceModel.SimBriefID;

            if (serviceModel.RefuelUnit == "KGS")
            {
                unitKGS.IsChecked = true;
                unitLBS.IsChecked = false;
            }
            else
            {
                unitKGS.IsChecked = false;
                unitLBS.IsChecked = true;
            }

            if (serviceModel.UseAcars)
            {
                acarsHoppie.IsEnabled = true;
                acarsSayIntentions.IsEnabled = true;

                if (serviceModel.AcarsNetwork == "Hoppie")
                {
                    acarsHoppie.IsChecked = true;
                    acarsSayIntentions.IsChecked = false;
                }
                else
                {
                    acarsHoppie.IsChecked= false;
                    acarsSayIntentions.IsChecked = true;
                }
            }
            else
            {
                acarsHoppie.IsEnabled = false;
                acarsSayIntentions.IsEnabled = false;
            }
        }

        
        private void acars_Click(object sender, RoutedEventArgs e)
        {
            if (sender == acarsHoppie)
            {
                serviceModel.SetSetting("acarsNetwork", "Hoppie");
            }
            else if (sender == acarsSayIntentions)
            {
                serviceModel.SetSetting("acarsNetwork", "SayIntentions");
            }
        }

        private void chkAutoBoard_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("autoBoarding", chkAutoBoard.IsChecked.ToString().ToLower());
        }

        private void chkAutoConnect_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("autoConnect", chkAutoConnect.IsChecked.ToString().ToLower());
        }
        
        private void chkJetwayOnly_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("jetwayOnly", chkJetwayOnly.IsChecked.ToString().ToLower());
        }

        private void chkAutoDeboard_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("autoDeboarding", chkAutoDeboard.IsChecked.ToString().ToLower());
        }

        private void chkAutoRefuel_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("autoRefuel", chkAutoRefuel.IsChecked.ToString().ToLower());
        }

        private void chkAutoReposition_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("repositionPlane", chkAutoReposition.IsChecked.ToString().ToLower());
        }

        private void chkCallCatering_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("callCatering", chkCallCatering.IsChecked.ToString().ToLower());
        }

        private void chkConnectPCA_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("connectPCA", chkConnectPCA.IsChecked.ToString().ToLower());
        }

        private void chkDisableCrewBoarding_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("disableCrew", chkDisableCrewBoarding.IsChecked.ToString().ToLower());
        }

        private void chkOpenDoorCatering_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("setOpenAftDoorCatering", chkOpenDoorCatering.IsChecked.ToString().ToLower());
        }
        
        private void chkOpenCargoDoors_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("setOpenCargoDoors", chkOpenCargoDoors.IsChecked.ToString().ToLower());
        }
        
        private void chkPcaOnlyJetway_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("pcaOnlyJetway", chkPcaOnlyJetway.IsChecked.ToString().ToLower());
        }

        private void chkSaveFuel_Checked(object sender, RoutedEventArgs e)
        {
            chkZeroFuel.IsChecked = false;
            chkZeroFuel.IsEnabled = false;
            serviceModel.SetSetting("setZeroFuel", chkZeroFuel.IsChecked.ToString().ToLower());
            serviceModel.SetSetting("setSaveFuel", chkSaveFuel.IsChecked.ToString().ToLower());
        }

        private void chkSaveFuel_unChecked(object sender, RoutedEventArgs e)
        {
            chkZeroFuel.IsEnabled = true;
            serviceModel.SetSetting("setSaveFuel", chkSaveFuel.IsChecked.ToString().ToLower());
        }

        public void chkSaveProsimFluidsOnArrival_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("saveHydraulicFluids", chkSaveProsimFluidsOnArrival.IsChecked.ToString().ToLower());
        }

        private void chkSynchBypass_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("synchBypass", chkSynchBypass.IsChecked.ToString().ToLower());
        }
        private void chkUseAcars_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("useAcars", chkUseAcars.IsChecked.ToString().ToLower());
        }

        private void chkUseAcars_Checked(object sender, RoutedEventArgs e)
        {
            acarsHoppie.IsEnabled = true;
            acarsSayIntentions.IsEnabled = true;
            serviceModel.SetSetting("useAcars", chkUseAcars.IsChecked.ToString().ToLower());
            if (serviceModel.AcarsNetwork == "Hoppie")
            {
                acarsHoppie.IsChecked = true;
                acarsSayIntentions.IsChecked = false;
            }
            else
            {
                acarsHoppie.IsChecked = false;
                acarsSayIntentions.IsChecked = true;
            }
        }

        private void chkUseAcars_unChecked(object sender, RoutedEventArgs e)
        {
            acarsHoppie.IsEnabled = false;
            acarsSayIntentions.IsEnabled = false;
            serviceModel.SetSetting("useAcars", chkUseAcars.IsChecked.ToString().ToLower());
        }

        private void chkUseActualPaxValue_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("useActualValue", chkUseActualPaxValue.IsChecked.ToString().ToLower());
        }

        private void chkZeroFuel_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("setZeroFuel", chkZeroFuel.IsChecked.ToString().ToLower());
        }

        private void flightPlan_Click(object sender, RoutedEventArgs e)
        {
            if (sender == eFBPlan && serviceModel.FlightPlanType == "MCDU")
            {
                serviceModel.SetSetting("flightPlanType", "EFB");
            }
            else if (sender == mCDUPlan && serviceModel.FlightPlanType == "EFB")
            {
                serviceModel.SetSetting("flightPlanType", "MCDU");
            }
        }
        
        private void txtRefuelRate_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter || e.Key != System.Windows.Input.Key.Return)
                return;

            txtRefuelRate_Set();
        }

        private void txtRefuelRate_LostFocus(object sender, RoutedEventArgs e)
        {
            txtRefuelRate_Set();
        }

        private void txtRefuelRate_Set()
        {
            if (float.TryParse(txtRefuelRate.Text, CultureInfo.InvariantCulture, out _))
                serviceModel.SetSetting("refuelRate", Convert.ToString(txtRefuelRate.Text, CultureInfo.InvariantCulture));
        }

        private void txtRepositionDelay_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter || e.Key != System.Windows.Input.Key.Return)
                return;

            txtRepositionDelay_Set();
        }

        private void txtSimbriefID_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Return)
                txtSimbriefID_Set();
        }

        private void txtSimbriefID_LostFocus(object sender, RoutedEventArgs e)
        {
            txtSimbriefID_Set();
        }

        private void txtSimbriefID_Set()
        {
            if (txtSimbriefID?.Text != null)
            {
                string id = txtSimbriefID.Text.Trim();
                
                // Validate the ID
                if (string.IsNullOrWhiteSpace(id) || id == "0" || !int.TryParse(id, out _))
                {
                    // Show immediate feedback
                    txtSimbriefID.Background = new SolidColorBrush(Colors.MistyRose);
                    txtSimbriefID.BorderBrush = new SolidColorBrush(Colors.Red);
                    
                    MessageBox.Show(
                        "Please enter a valid Simbrief ID (numeric value).",
                        "Invalid Simbrief ID",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                        
                    return;
                }
                
                // Reset styling if valid
                txtSimbriefID.Background = new SolidColorBrush(Colors.White);
                txtSimbriefID.BorderBrush = new SolidColorBrush(Colors.Gray);
                
                // Save the valid ID
                serviceModel.SetSetting("pilotID", id);
                
                // Provide positive feedback
                LogService.Log(LogLevel.Information, "MainWindow", $"Simbrief ID set to {id}");
                
                // If we have a valid ID and there's a pending flight plan load, trigger a retry
                if (serviceModel.IsValidSimbriefId())
                {
                    EventAggregator.Instance.Publish(new RetryFlightPlanLoadEvent());
                }
            }
        }

        private void txtRepositionDelay_LostFocus(object sender, RoutedEventArgs e)
        {
            txtRepositionDelay_Set();
        }

        private void txtRepositionDelay_Set()
        {
            if (float.TryParse(txtRepositionDelay.Text, CultureInfo.InvariantCulture, out _))
                serviceModel.SetSetting("repositionDelay", Convert.ToString(txtRepositionDelay.Text, CultureInfo.InvariantCulture));
        }

        private void units_Click(object sender, RoutedEventArgs e)
        {
            if (!float.TryParse(txtRefuelRate.Text, CultureInfo.InvariantCulture, out float fuelRate))
                return;

            if (sender == unitKGS && serviceModel.RefuelUnit == "LBS")
            {
                fuelRate /= ServiceLocator.WeightConversion;
                serviceModel.SetSetting("refuelRate", Convert.ToString(fuelRate, CultureInfo.InvariantCulture));
                serviceModel.SetSetting("refuelUnit", "KGS");
                txtRefuelRate.Text = Convert.ToString(fuelRate, CultureInfo.InvariantCulture);
            }
            else if (sender == unitLBS && serviceModel.RefuelUnit == "KGS")
            {
                fuelRate *= ServiceLocator.WeightConversion;
                serviceModel.SetSetting("refuelRate", Convert.ToString(fuelRate, CultureInfo.InvariantCulture));
                serviceModel.SetSetting("refuelUnit", "LBS");
                txtRefuelRate.Text = Convert.ToString(fuelRate, CultureInfo.InvariantCulture);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();

            // Clean up ViewModel resources
            _viewModel.Cleanup();
        }

        private void LoadThemes()
        {
            try
            {
                // Clear existing items
                cboTheme.Items.Clear();
                
                // Add available themes
                foreach (string themeName in ThemeManager.Instance.AvailableThemes)
                {
                    cboTheme.Items.Add(themeName);
                }
                
                // Select current theme
                if (ThemeManager.Instance.CurrentTheme != null)
                {
                    cboTheme.SelectedItem = ThemeManager.Instance.CurrentTheme.Name;
                }
                
                // Show themes directory path
                txtThemesPath.Text = Path.Combine(App.AppDir, "Themes");
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Warning, "MainWindow", $"Error loading themes: {ex.Message}");
            }
        }

        private void cboTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cboTheme.SelectedItem != null)
                {
                    string themeName = cboTheme.SelectedItem.ToString();
                    ThemeManager.Instance.ApplyTheme(themeName);
                }
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Warning, "MainWindow", $"Error changing theme: {ex.Message}");
            }
        }

        private void btnRefreshThemes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ThemeManager.Instance.RefreshThemes();
                LoadThemes();
            }
            catch (Exception ex)
            {
                LogService.Log(LogLevel.Warning, "MainWindow", $"Error refreshing themes: {ex.Message}");
            }
        }

        protected void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible)
            {
                notifyModel.CanExecuteHideWindow = false;
                notifyModel.CanExecuteShowWindow = true;

                // Notify ViewModel that window is hidden
                _viewModel.OnWindowHidden();
            }
            else
            {
                LoadSettings();
                LoadThemes(); // Load available themes

                // Notify ViewModel that window is visible
                _viewModel.OnWindowVisible();
            }
        }

        private async void btnTestSimbriefConnection_Click(object sender, RoutedEventArgs e)
        {
            if (!serviceModel.IsValidSimbriefId())
            {
                MessageBox.Show(
                    "Please enter a valid Simbrief ID first.",
                    "Invalid Simbrief ID",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            
            btnTestSimbriefConnection.IsEnabled = false;
            btnTestSimbriefConnection.Content = "Testing...";
            
            try
            {
                // Create a temporary FlightPlan object to test the connection
                var testPlan = new FlightPlan(serviceModel);
                var result = await System.Threading.Tasks.Task.Run(() => testPlan.LoadWithValidation());
                
                if (result == FlightPlan.LoadResult.Success)
                {
                    MessageBox.Show(
                        $"Successfully connected to Simbrief with ID: {serviceModel.SimBriefID}\n\n" +
                        $"Flight: {testPlan.Flight}\n" +
                        $"Route: {testPlan.Origin} → {testPlan.Destination}",
                        "Connection Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Failed to connect to Simbrief. Please check your ID and internet connection.",
                        "Connection Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error testing connection: {ex.Message}",
                    "Connection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                btnTestSimbriefConnection.IsEnabled = true;
                btnTestSimbriefConnection.Content = "Test Connection";
            }
        }

        private void btnConfigureExternalDependencies_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ExternalDependenciesDialog(serviceModel);
            dialog.Owner = this;
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                // No need for explicit save - settings are saved via SetSetting

                // Show restart message
                MessageBox.Show(
                    "The external dependencies configuration has been updated. Please restart the application for the changes to take effect.",
                    "Configuration Updated",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void SetDebugVerbosityComboBox()
        {
            string verbosity = serviceModel.DebugLogVerbosity;

            // First check for predefined values
            foreach (ComboBoxItem item in cboDebugVerbosity.Items)
            {
                if (item.Tag.ToString() == verbosity)
                {
                    cboDebugVerbosity.SelectedItem = item;
                    // Add null check here too
                    if (pnlCustomVerbosity != null)
                    {
                        pnlCustomVerbosity.Visibility = Visibility.Collapsed;
                    }
                    return;
                }
            }

            // If not found, it's a custom list
            foreach (ComboBoxItem item in cboDebugVerbosity.Items)
            {
                if (item.Tag.ToString() == "Custom")
                {
                    cboDebugVerbosity.SelectedItem = item;
                    // Add null check here too
                    if (pnlCustomVerbosity != null && txtCustomVerbosity != null)
                    {
                        txtCustomVerbosity.Text = verbosity;
                        pnlCustomVerbosity.Visibility = Visibility.Visible;
                    }
                    return;
                }
            }
        }

        private void cboDebugVerbosity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboDebugVerbosity.SelectedItem is ComboBoxItem item)
            {
                string tag = item.Tag.ToString();

                // Add null check before accessing the panel
                if (pnlCustomVerbosity != null)
                {
                    // Show/hide custom panel if "Custom" is selected
                    if (tag == "Custom")
                    {
                        pnlCustomVerbosity.Visibility = Visibility.Visible;

                        // Don't update settings yet - wait for custom text entry
                        return;
                    }
                    else
                    {
                        pnlCustomVerbosity.Visibility = Visibility.Collapsed;

                        // Update settings with predefined value
                        serviceModel.SetSetting("debugLogVerbosity", tag);
                        serviceModel.DebugLogVerbosity = tag;

                        // Update the log service
                        LogService.SetDebugVerbosity(tag);
                    }
                }
            }
        }

        private void txtCustomVerbosity_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && txtCustomVerbosity != null)
            {
                UpdateCustomVerbosity();
            }
        }

        private void txtCustomVerbosity_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txtCustomVerbosity != null)
            {
                UpdateCustomVerbosity();
            }
        }

        private void UpdateCustomVerbosity()
        {
            if (txtCustomVerbosity != null)
            {
                string customValue = txtCustomVerbosity.Text.Trim();

                // Verify the custom input has valid categories
                bool isValid = VerifyCustomCategories(customValue);

                if (isValid)
                {
                    // Update settings
                    serviceModel.SetSetting("debugLogVerbosity", customValue);
                    serviceModel.DebugLogVerbosity = customValue;

                    // Update the log service
                    LogService.SetDebugVerbosity(customValue);

                    // Provide feedback
                    LogService.Log(LogLevel.Information, nameof(MainWindow),
                        $"Debug verbosity set to: {customValue}", LogCategory.All);
                }
                else
                {
                    // Provide feedback for invalid categories
                    MessageBox.Show(
                        "One or more category names are invalid. Please check the list of available categories.",
                        "Invalid Categories",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }

        private bool VerifyCustomCategories(string categoriesString)
        {
            if (string.IsNullOrWhiteSpace(categoriesString) ||
                categoriesString.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string[] categories = categoriesString.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (string category in categories)
            {
                string trimmed = category.Trim();

                // Try parse directly
                bool isValid = Enum.TryParse<LogCategory>(trimmed, true, out _);

                // Check known friendly names if direct parse fails
                if (!isValid)
                {
                    isValid = IsKnownFriendlyName(trimmed);
                }

                if (!isValid)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsKnownFriendlyName(string name)
        {
            string lowered = name.ToLowerInvariant();

            return lowered switch
            {
                "all" or "all categories" or "gsx" or "gsxcontroller" or "refuel" or "refueling" or
                "board" or "boarding" or "cater" or "catering" or "ground" or "groundservices" or
                "ground services" or "sim" or "simconnect" or "ps" or "prosim" or "event" or
                "events" or "menu" or "menus" or "audio" or "sound" or "config" or "configuration" or
                "door" or "doors" or "cargo" or "load" or "loadsheet" => true,
                _ => false
            };
        }
    }
}
