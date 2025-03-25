﻿using System;
using System.Globalization;
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
            // Update MSFS status indicator
            MsfsStatusIndicator.Fill = serviceModel.IsSimRunning 
                ? new SolidColorBrush(Colors.Green) 
                : new SolidColorBrush(Colors.Red);

            // Update SimConnect status indicator
            SimConnectStatusIndicator.Fill = (IPCManager.SimConnect != null && IPCManager.SimConnect.IsReady) 
                ? new SolidColorBrush(Colors.Green) 
                : new SolidColorBrush(Colors.Red);

            // Update Prosim status indicator
            ProsimStatusIndicator.Fill = serviceModel.IsProsimRunning 
                ? new SolidColorBrush(Colors.Green) 
                : new SolidColorBrush(Colors.Red);

            // Update Session status indicator
            SessionStatusIndicator.Fill = serviceModel.IsSessionRunning 
                ? new SolidColorBrush(Colors.Green) 
                : new SolidColorBrush(Colors.Red);
        
            // Update current date/time
            CurrentDateTime.Text = DateTime.Now.ToString("dd.MM.yyyy");
        
            // Update flight phase display
            UpdateFlightPhaseDisplay();
    
            // Update ground services status
            UpdateGroundServicesStatus();
        }

        /// <summary>
        /// Updates the ground services status indicators based on the current state
        /// </summary>
        protected void UpdateGroundServicesStatus()
        {
            // If SimConnect is not available, set all indicators to gray
            if (IPCManager.SimConnect == null || !IPCManager.SimConnect.IsReady)
            {
                SetAllGroundServiceIndicators(Colors.LightGray);
                return;
            }
    
            var simConnect = IPCManager.SimConnect;
    
            // Jetway status
            JetwayStatusIndicator.Fill = new SolidColorBrush(
                simConnect.ReadLvar("FSDT_GSX_JETWAY") == 5 ? Colors.Green : 
                simConnect.ReadLvar("FSDT_GSX_OPERATEJETWAYS_STATE") >= 3 ? Colors.Gold : 
                Colors.LightGray);
    
            // Stairs status
            StairsStatusIndicator.Fill = new SolidColorBrush(
                simConnect.ReadLvar("FSDT_GSX_STAIRS") == 5 ? Colors.Green : 
                simConnect.ReadLvar("FSDT_GSX_OPERATESTAIRS_STATE") >= 3 ? Colors.Gold : 
                Colors.LightGray);
    
            // Refueling status
            float refuelState = simConnect.ReadLvar("FSDT_GSX_REFUELING_STATE");
            RefuelStatusIndicator.Fill = new SolidColorBrush(
                refuelState == 6 ? Colors.Green : 
                refuelState == 5 ? Colors.Gold : 
                Colors.LightGray);
    
            // Catering status
            float cateringState = simConnect.ReadLvar("FSDT_GSX_CATERING_STATE");
            CateringStatusIndicator.Fill = new SolidColorBrush(
                cateringState == 6 ? Colors.Green : 
                cateringState >= 4 ? Colors.Gold : 
                Colors.LightGray);
    
            // Boarding status
            float boardingState = simConnect.ReadLvar("FSDT_GSX_BOARDING_STATE");
            BoardingStatusIndicator.Fill = new SolidColorBrush(
                boardingState == 6 ? Colors.Green : 
                boardingState >= 4 ? Colors.Gold : 
                Colors.LightGray);
    
            // Deboarding status
            float deboardingState = simConnect.ReadLvar("FSDT_GSX_DEBOARDING_STATE");
            DeboardingStatusIndicator.Fill = new SolidColorBrush(
                deboardingState == 6 ? Colors.Green : 
                deboardingState >= 4 ? Colors.Gold : 
                Colors.LightGray);
    
            // GPU status - use ProsimController if available
/*
            if (IPCManager.ProsimController != null)
            {
                GPUStatusIndicator.Fill = new SolidColorBrush(
                    IPCManager.ProsimController.GetServiceGPU() ? Colors.Green : Colors.LightGray);

                // PCA status
                PCAStatusIndicator.Fill = new SolidColorBrush(
                    IPCManager.ProsimController.GetServicePCA() ? Colors.Green : Colors.LightGray);
            
                // Chocks status
                ChocksStatusIndicator.Fill = new SolidColorBrush(
                    IPCManager.ProsimController.GetServiceChocks() ? Colors.Green : Colors.LightGray);
            }
            else
            {
                // Fallback to gray if ProsimController is not available
                GPUStatusIndicator.Fill = new SolidColorBrush(Colors.LightGray);
                PCAStatusIndicator.Fill = new SolidColorBrush(Colors.LightGray);
                ChocksStatusIndicator.Fill = new SolidColorBrush(Colors.LightGray);
            }
*/           
            // Pushback status
            float departureState = simConnect.ReadLvar("FSDT_GSX_DEPARTURE_STATE");
            PushbackStatusIndicator.Fill = new SolidColorBrush(
                departureState == 6 ? Colors.Green : 
                departureState >= 4 ? Colors.Gold : 
                Colors.LightGray);
        }

        /// <summary>
        /// Sets all ground service indicators to the specified color
        /// </summary>
        private void SetAllGroundServiceIndicators(Color color)
        {
            var brush = new SolidColorBrush(color);
            JetwayStatusIndicator.Fill = brush;
            StairsStatusIndicator.Fill = brush;
            RefuelStatusIndicator.Fill = brush;
            CateringStatusIndicator.Fill = brush;
            BoardingStatusIndicator.Fill = brush;
            DeboardingStatusIndicator.Fill = brush;
            GPUStatusIndicator.Fill = brush;
            PCAStatusIndicator.Fill = brush;
            PushbackStatusIndicator.Fill = brush;
            ChocksStatusIndicator.Fill = brush;
        }
        
        /// <summary>
        /// Updates the flight phase display with the current phase and appropriate color
        /// </summary>
        protected void UpdateFlightPhaseDisplay()
        {
            if (IPCManager.GsxController == null)
            {
                lblFlightPhase.Content = "NO PHASE";
                lblFlightPhase.Foreground = new SolidColorBrush(Colors.Gray);
        
                // Reset all progress bar sections to inactive
                ResetFlightPhaseProgressBar();
                return;
            }
    
            // Get the current flight state from GsxController
            FlightState currentState = IPCManager.GsxController.CurrentFlightState;
    
            // Reset all progress bar sections to inactive
            ResetFlightPhaseProgressBar();
    
            // Set the label text and color based on the current flight state
            // Also highlight the appropriate section in the progress bar
            switch (currentState)
            {
                case FlightState.PREFLIGHT:
                case FlightState.DEPARTURE:
                    lblFlightPhase.Content = "AT GATE";
                    lblFlightPhase.Foreground = new SolidColorBrush(Colors.RoyalBlue);
                    // Highlight the first section of the progress bar
                    HighlightFlightPhaseSection(0);
                    break;
                case FlightState.TAXIOUT:
                    lblFlightPhase.Content = "TAXI OUT";
                    lblFlightPhase.Foreground = new SolidColorBrush(Colors.Gold);
                    // Highlight the second section of the progress bar
                    HighlightFlightPhaseSection(1);
                    break;
                case FlightState.FLIGHT:
                    lblFlightPhase.Content = "IN FLIGHT";
                    lblFlightPhase.Foreground = new SolidColorBrush(Colors.Green);
                    // Highlight the third section of the progress bar
                    HighlightFlightPhaseSection(2);
                    break;
                case FlightState.TAXIIN:
                case FlightState.ARRIVAL:
                    lblFlightPhase.Content = "APPROACH";
                    lblFlightPhase.Foreground = new SolidColorBrush(Colors.Purple);
                    // Highlight the fourth section of the progress bar
                    HighlightFlightPhaseSection(3);
                    break;
                case FlightState.TURNAROUND:
                    lblFlightPhase.Content = "ARRIVED";
                    lblFlightPhase.Foreground = new SolidColorBrush(Colors.Teal);
                    // Highlight the fifth section of the progress bar
                    HighlightFlightPhaseSection(4);
                    break;
                default:
                    lblFlightPhase.Content = "UNKNOWN";
                    lblFlightPhase.Foreground = new SolidColorBrush(Colors.Gray);
                    break;
            }
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
                timer.Start();
            }
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
