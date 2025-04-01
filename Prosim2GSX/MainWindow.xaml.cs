﻿using Prosim2GSX.Events;
using Prosim2GSX.Themes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Prosim2GSX.Models;

namespace Prosim2GSX
{
    public partial class MainWindow : Window
    {
        protected NotifyIconViewModel notifyModel;
        protected ServiceModel serviceModel;
        protected DispatcherTimer timer;
        protected int lineCounter = 0;
        private List<SubscriptionToken> _subscriptionTokens = new List<SubscriptionToken>();

        public MainWindow(NotifyIconViewModel notifyModel, ServiceModel serviceModel)
        {
            InitializeComponent();
            this.notifyModel = notifyModel;
            this.serviceModel = serviceModel;

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += OnTick;
            
            // Subscribe to events
            SubscribeToEvents();
        }
        
        private void SubscribeToEvents()
        {
            // Subscribe to connection status events
            _subscriptionTokens.Add(EventAggregator.Instance.Subscribe<ConnectionStatusChangedEvent>(OnConnectionStatusChanged));
            
            // Subscribe to service status events
            _subscriptionTokens.Add(EventAggregator.Instance.Subscribe<ServiceStatusChangedEvent>(OnServiceStatusChanged));
            
            // Subscribe to flight phase events
            _subscriptionTokens.Add(EventAggregator.Instance.Subscribe<FlightPhaseChangedEvent>(OnFlightPhaseChanged));
        }
        
        private void OnConnectionStatusChanged(ConnectionStatusChangedEvent evt)
        {
            // Update UI based on connection status
            Dispatcher.Invoke(() => {
                switch (evt.ConnectionName)
                {
                    case "MSFS":
                        MsfsStatusIndicator.Fill = evt.IsConnected ? 
                            new SolidColorBrush(Colors.Green) : 
                            new SolidColorBrush(Colors.Red);
                        break;
                    case "SimConnect":
                        SimConnectStatusIndicator.Fill = evt.IsConnected ? 
                            new SolidColorBrush(Colors.Green) : 
                            new SolidColorBrush(Colors.Red);
                        break;
                    case "Prosim":
                        ProsimStatusIndicator.Fill = evt.IsConnected ? 
                            new SolidColorBrush(Colors.Green) : 
                            new SolidColorBrush(Colors.Red);
                        break;
                    case "Session":
                        SessionStatusIndicator.Fill = evt.IsConnected ? 
                            new SolidColorBrush(Colors.Green) : 
                            new SolidColorBrush(Colors.Red);
                        break;
                }
            });
        }
        
        private void OnServiceStatusChanged(ServiceStatusChangedEvent evt)
        {
            // Update UI based on service status
            Dispatcher.Invoke(() => {
                SolidColorBrush brush;
                switch (evt.Status)
                {
                    case ServiceStatus.Completed:
                        brush = new SolidColorBrush(Colors.Green);
                        break;
                    case ServiceStatus.Active:
                        brush = new SolidColorBrush(Colors.Gold);
                        break;
                    case ServiceStatus.Waiting:
                        brush = new SolidColorBrush(Colors.Blue);
                        break;
                    case ServiceStatus.Requested:
                        brush = new SolidColorBrush(Colors.Blue);
                        break;
                    case ServiceStatus.Disconnected:
                        brush = new SolidColorBrush(Colors.Red);
                        break;
                    default:
                        brush = new SolidColorBrush(Colors.LightGray);
                        break;
                }
                
                // Update the appropriate indicator
                switch (evt.ServiceName)
                {
                    case "Jetway":
                        JetwayStatusIndicator.Fill = brush;
                        break;
                    case "Stairs":
                        StairsStatusIndicator.Fill = brush;
                        break;
                    case "Refuel":
                        RefuelStatusIndicator.Fill = brush;
                        break;
                    case "Catering":
                        CateringStatusIndicator.Fill = brush;
                        break;
                    case "Boarding":
                        BoardingStatusIndicator.Fill = brush;
                        break;
                    case "Deboarding":
                        DeboardingStatusIndicator.Fill = brush;
                        break;
                    case "GPU":
                        GPUStatusIndicator.Fill = brush;
                        break;
                    case "PCA":
                        PCAStatusIndicator.Fill = brush;
                        break;
                    case "Pushback":
                        PushbackStatusIndicator.Fill = brush;
                        break;
                    case "Chocks":
                        ChocksStatusIndicator.Fill = brush;
                        break;
                }
            });
        }
        
        private void OnFlightPhaseChanged(FlightPhaseChangedEvent evt)
        {
            // Update flight phase display
            Dispatcher.Invoke(() => {
                switch (evt.NewState)
                {
                    case FlightState.PREFLIGHT:
                    case FlightState.DEPARTURE:
                        lblFlightPhase.Content = "AT GATE";
                        lblFlightPhase.Foreground = new SolidColorBrush(Colors.RoyalBlue);
                        HighlightFlightPhaseSection(0);
                        break;
                    case FlightState.TAXIOUT:
                        lblFlightPhase.Content = "TAXI OUT";
                        lblFlightPhase.Foreground = new SolidColorBrush(Colors.Gold);
                        HighlightFlightPhaseSection(1);
                        break;
                    case FlightState.FLIGHT:
                        lblFlightPhase.Content = "IN FLIGHT";
                        lblFlightPhase.Foreground = new SolidColorBrush(Colors.Green);
                        HighlightFlightPhaseSection(2);
                        break;
                    case FlightState.TAXIIN:
                    case FlightState.ARRIVAL:
                        lblFlightPhase.Content = "APPROACH";
                        lblFlightPhase.Foreground = new SolidColorBrush(Colors.Purple);
                        HighlightFlightPhaseSection(3);
                        break;
                    case FlightState.TURNAROUND:
                        lblFlightPhase.Content = "ARRIVED";
                        lblFlightPhase.Foreground = new SolidColorBrush(Colors.Teal);
                        HighlightFlightPhaseSection(4);
                        break;
                    default:
                        lblFlightPhase.Content = "UNKNOWN";
                        lblFlightPhase.Foreground = new SolidColorBrush(Colors.Gray);
                        break;
                }
            });
        }

        protected void LoadSettings()
        {
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
            chkGsxVolumeControl.IsChecked = serviceModel.GsxVolumeControl;
            chkOpenDoorCatering.IsChecked = serviceModel.SetOpenCateringDoor;
            chkOpenCargoDoors.IsChecked = serviceModel.SetOpenCargoDoors;
            chkPcaOnlyJetway.IsChecked = serviceModel.PcaOnlyJetways;
            chkSynchBypass.IsChecked = serviceModel.SynchBypass;
            chkSaveFuel.IsChecked = serviceModel.SetSaveFuel;
            chkUseAcars.IsChecked = serviceModel.UseAcars;
            chkUseActualPaxValue.IsChecked = serviceModel.UseActualPaxValue;
            chkVhf1LatchMute.IsChecked = serviceModel.Vhf1LatchMute;
            chkVhf1VolumeControl.IsChecked = serviceModel.Vhf1VolumeControl;
            chkZeroFuel.IsChecked = serviceModel.SetZeroFuel;

            txtRefuelRate.Text = serviceModel.RefuelRate.ToString(CultureInfo.InvariantCulture);
            txtRepositionDelay.Text = serviceModel.RepositionDelay.ToString(CultureInfo.InvariantCulture);

            txtVhf1VolumeApp.IsEnabled = serviceModel.Vhf1VolumeControl;
            txtVhf1VolumeApp.Text = serviceModel.Vhf1VolumeApp;


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

        protected void UpdateLogArea()
        {
            while (Logger.MessageQueue.Count > 0)
            {

                if (lineCounter > 5)
                    txtLogMessages.Text = txtLogMessages.Text[(txtLogMessages.Text.IndexOf('\n') + 1)..];
                txtLogMessages.Text += Logger.MessageQueue.Dequeue().ToString() + "\n";
                lineCounter++;
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


        private void chkGsxVolumeControl_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("gsxVolumeControl", chkGsxVolumeControl.IsChecked.ToString().ToLower());
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


        private void chkVhf1VolumeControl_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("vhf1VolumeControl", chkVhf1VolumeControl.IsChecked.ToString().ToLower());
            txtVhf1VolumeApp.IsEnabled = (bool)chkVhf1VolumeControl.IsChecked;
        }

        private void chkVhf1LatchMute_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("vhf1LatchMute", chkVhf1LatchMute.IsChecked.ToString().ToLower());
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

        protected void OnTick(object sender, EventArgs e)
        {
            UpdateLogArea();
            UpdateStatus();
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

        private void txtVhf1VolumeApp_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter || e.Key != System.Windows.Input.Key.Return)
                return;

            txtVhf1VolumeApp_Set();
        }

        private void txtVhf1VolumeApp_LostFocus(object sender, RoutedEventArgs e)
        {
            txtVhf1VolumeApp_Set();
        }

        private void txtVhf1VolumeApp_Set()
        {
            if (txtVhf1VolumeApp?.Text != null)
                serviceModel.SetSetting("vhf1VolumeApp", txtVhf1VolumeApp.Text);
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
                fuelRate /= ProsimController.weightConversion;
                serviceModel.SetSetting("refuelRate", Convert.ToString(fuelRate, CultureInfo.InvariantCulture));
                serviceModel.SetSetting("refuelUnit", "KGS");
                txtRefuelRate.Text = Convert.ToString(fuelRate, CultureInfo.InvariantCulture);
            }
            else if (sender == unitLBS && serviceModel.RefuelUnit == "KGS")
            {
                fuelRate *= ProsimController.weightConversion;
                serviceModel.SetSetting("refuelRate", Convert.ToString(fuelRate, CultureInfo.InvariantCulture));
                serviceModel.SetSetting("refuelUnit", "LBS");
                txtRefuelRate.Text = Convert.ToString(fuelRate, CultureInfo.InvariantCulture);
            }
        }

        protected void UpdateStatus()
        {
            // Update current date/time
            CurrentDateTime.Text = DateTime.Now.ToString("dd.MM.yyyy");
            
            // Note: Connection status, service status, and flight phase are now updated via events
        }

        /// <summary>
        /// Resets all flight phase progress bar sections to inactive (gray)
        /// </summary>
        private void ResetFlightPhaseProgressBar()
        {
            // Get all the Border elements in the progress bar
            var grid = progressBar;
            if (grid != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    var border = (Border)grid.Children[i];
                    border.Background = new SolidColorBrush(Colors.LightGray);
                }
            }
        }

        /// <summary>
        /// Highlights a specific section of the flight phase progress bar
        /// </summary>
        /// <param name="sectionIndex">Index of the section to highlight (0-4)</param>
        private void HighlightFlightPhaseSection(int sectionIndex)
        {
            // Get the Grid that contains the progress bar sections
            var grid = progressBar;
            if (grid != null && sectionIndex >= 0 && sectionIndex < 5)
            {
                // Highlight the specified section
                var border = (Border)grid.Children[sectionIndex];
                border.Background = new SolidColorBrush(Colors.DodgerBlue);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            
            // Unsubscribe from all events
            foreach (var token in _subscriptionTokens)
            {
                EventAggregator.Instance.Unsubscribe<EventBase>(token);
            }
            _subscriptionTokens.Clear();
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
                Logger.Log(LogLevel.Warning, "MainWindow", $"Error loading themes: {ex.Message}");
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
                Logger.Log(LogLevel.Warning, "MainWindow", $"Error changing theme: {ex.Message}");
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
                Logger.Log(LogLevel.Warning, "MainWindow", $"Error refreshing themes: {ex.Message}");
            }
        }
        
        protected void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible)
            {
                notifyModel.CanExecuteHideWindow = false;
                notifyModel.CanExecuteShowWindow = true;
                timer.Stop();
            }
            else
            {
                LoadSettings();
                LoadThemes(); // Load available themes
                
                // Directly update connection status indicators based on current model state
                MsfsStatusIndicator.Fill = serviceModel.IsSimRunning ?
                    new SolidColorBrush(Colors.Green) :
                    new SolidColorBrush(Colors.Red);

                ProsimStatusIndicator.Fill = serviceModel.IsProsimRunning ?
                    new SolidColorBrush(Colors.Green) :
                    new SolidColorBrush(Colors.Red);

                SimConnectStatusIndicator.Fill = IPCManager.SimConnect?.IsConnected == true ?
                    new SolidColorBrush(Colors.Green) :
                    new SolidColorBrush(Colors.Red);

                SessionStatusIndicator.Fill = serviceModel.IsSessionRunning ?
                    new SolidColorBrush(Colors.Green) :
                    new SolidColorBrush(Colors.Red);

                timer.Start();
            }
        }

        private void btnAudioSettings_Click(object sender, RoutedEventArgs e)
        {
            // Switch to the Audio Settings tab
            MainTabControl.SelectedItem = AudioSettingsTab;
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            // Switch to the Settings tab
            MainTabControl.SelectedItem = SettingsTab;
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            // Show help information
            MessageBox.Show(
                "Prosim2GSX provides integration between Prosim A320 and GSX Pro.\n\n" +
                "For more information, please refer to the documentation.",
                "Prosim2GSX Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }


    }
}
