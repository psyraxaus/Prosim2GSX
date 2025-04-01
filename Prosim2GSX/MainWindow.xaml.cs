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
using Prosim2GSX.Services.Audio;
using System.Threading;
using static Prosim2GSX.Services.Audio.AudioChannelConfig;

namespace Prosim2GSX
{
    public partial class MainWindow : Window
    {
        protected NotifyIconViewModel notifyModel;
        protected ServiceModel serviceModel;
        protected DispatcherTimer timer;
        protected int lineCounter = 0;
        private List<SubscriptionToken> _subscriptionTokens = new List<SubscriptionToken>();
        private bool _isLoadingSettings = false;
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
            // Set audio API radio buttons
            rbCoreAudio.IsChecked = serviceModel.AudioApiType == AudioApiType.CoreAudio;
            rbVoiceMeeter.IsChecked = serviceModel.AudioApiType == AudioApiType.VoiceMeeter;

            // Update UI visibility based on selected API
            UpdateAudioApiVisibility();

            // Set audio API radio buttons
            rbCoreAudio.IsChecked = serviceModel.AudioApiType == AudioApiType.CoreAudio;
            rbVoiceMeeter.IsChecked = serviceModel.AudioApiType == AudioApiType.VoiceMeeter;

            // Update UI visibility based on selected API
            UpdateAudioApiVisibility();

            // If VoiceMeeter is selected, load available strips
            if (serviceModel.AudioApiType == AudioApiType.VoiceMeeter)
            {
                LoadVoiceMeeterStrips();
            }

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
            chkIntVolumeControl.IsChecked = serviceModel.GsxVolumeControl;
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

            // INT settings
            chkIntVolumeControl.IsChecked = serviceModel.GsxVolumeControl;
            chkIntLatchMute.IsChecked = serviceModel.IntLatchMute;
            txtIntVolumeApp.Text = serviceModel.IntVolumeApp;
            txtIntVolumeApp.IsEnabled = serviceModel.GsxVolumeControl;

            // VHF1 settings
            chkVhf1VolumeControl.IsChecked = serviceModel.Vhf1VolumeControl;
            chkVhf1LatchMute.IsChecked = serviceModel.Vhf1LatchMute;
            txtVhf1VolumeApp.IsEnabled = serviceModel.Vhf1VolumeControl;
            txtVhf1VolumeApp.Text = serviceModel.Vhf1VolumeApp;

            // VHF2 settings
            chkVhf2VolumeControl.IsChecked = serviceModel.Vhf2VolumeControl;
            chkVhf2LatchMute.IsChecked = serviceModel.Vhf2LatchMute;
            txtVhf2VolumeApp.IsEnabled = serviceModel.Vhf2VolumeControl;
            txtVhf2VolumeApp.Text = serviceModel.Vhf2VolumeApp;

            // VHF3 settings
            chkVhf3VolumeControl.IsChecked = serviceModel.Vhf3VolumeControl;
            chkVhf3LatchMute.IsChecked = serviceModel.Vhf3LatchMute;
            txtVhf3VolumeApp.IsEnabled = serviceModel.Vhf3VolumeControl;
            txtVhf3VolumeApp.Text = serviceModel.Vhf3VolumeApp;

            // CAB settings
            chkCabVolumeControl.IsChecked = serviceModel.CabVolumeControl;
            chkCabLatchMute.IsChecked = serviceModel.CabLatchMute;
            txtCabVolumeApp.IsEnabled = serviceModel.CabVolumeControl;
            txtCabVolumeApp.Text = serviceModel.CabVolumeApp;

            // PA settings
            chkPaVolumeControl.IsChecked = serviceModel.PaVolumeControl;
            chkPaLatchMute.IsChecked = serviceModel.PaLatchMute;
            txtPaVolumeApp.IsEnabled = serviceModel.PaVolumeControl;
            txtPaVolumeApp.Text = serviceModel.PaVolumeApp;


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


        private void chkIntVolumeControl_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("gsxVolumeControl", chkIntVolumeControl.IsChecked.ToString().ToLower());
            txtIntVolumeApp.IsEnabled = (bool)chkIntVolumeControl.IsChecked;
        }

        private void chkIntLatchMute_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("intLatchMute", chkIntLatchMute.IsChecked.ToString().ToLower());
        }

        private void txtIntVolumeApp_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter || e.Key != System.Windows.Input.Key.Return)
                return;

            txtIntVolumeApp_Set();
        }

        private void txtIntVolumeApp_LostFocus(object sender, RoutedEventArgs e)
        {
            txtIntVolumeApp_Set();
        }

        private void txtIntVolumeApp_Set()
        {
            if (txtIntVolumeApp?.Text != null)
                serviceModel.SetSetting("gsxVolumeApp", txtIntVolumeApp.Text);
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

        private void chkVhf2VolumeControl_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("vhf2VolumeControl", chkVhf2VolumeControl.IsChecked.ToString().ToLower());
            txtVhf2VolumeApp.IsEnabled = (bool)chkVhf2VolumeControl.IsChecked;
        }

        private void chkVhf2LatchMute_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("vhf2LatchMute", chkVhf2LatchMute.IsChecked.ToString().ToLower());
        }

        private void txtVhf2VolumeApp_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter || e.Key != System.Windows.Input.Key.Return)
                return;

            txtVhf2VolumeApp_Set();
        }

        private void txtVhf2VolumeApp_LostFocus(object sender, RoutedEventArgs e)
        {
            txtVhf2VolumeApp_Set();
        }

        private void txtVhf2VolumeApp_Set()
        {
            if (txtVhf2VolumeApp?.Text != null)
                serviceModel.SetSetting("vhf2VolumeApp", txtVhf2VolumeApp.Text);
        }

        // Add these event handlers for VHF3
        private void chkVhf3VolumeControl_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("vhf3VolumeControl", chkVhf3VolumeControl.IsChecked.ToString().ToLower());
            txtVhf3VolumeApp.IsEnabled = (bool)chkVhf3VolumeControl.IsChecked;
        }

        private void chkVhf3LatchMute_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("vhf3LatchMute", chkVhf3LatchMute.IsChecked.ToString().ToLower());
        }

        private void txtVhf3VolumeApp_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter || e.Key != System.Windows.Input.Key.Return)
                return;

            txtVhf3VolumeApp_Set();
        }

        private void txtVhf3VolumeApp_LostFocus(object sender, RoutedEventArgs e)
        {
            txtVhf3VolumeApp_Set();
        }

        private void txtVhf3VolumeApp_Set()
        {
            if (txtVhf3VolumeApp?.Text != null)
                serviceModel.SetSetting("vhf3VolumeApp", txtVhf3VolumeApp.Text);
        }

        // Add these event handlers for CAB
        private void chkCabVolumeControl_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("cabVolumeControl", chkCabVolumeControl.IsChecked.ToString().ToLower());
            txtCabVolumeApp.IsEnabled = (bool)chkCabVolumeControl.IsChecked;
        }

        private void chkCabLatchMute_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("cabLatchMute", chkCabLatchMute.IsChecked.ToString().ToLower());
        }

        private void txtCabVolumeApp_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter || e.Key != System.Windows.Input.Key.Return)
                return;

            txtCabVolumeApp_Set();
        }

        private void txtCabVolumeApp_LostFocus(object sender, RoutedEventArgs e)
        {
            txtCabVolumeApp_Set();
        }

        private void txtCabVolumeApp_Set()
        {
            if (txtCabVolumeApp?.Text != null)
                serviceModel.SetSetting("cabVolumeApp", txtCabVolumeApp.Text);
        }

        // Add these event handlers for PA
        private void chkPaVolumeControl_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("paVolumeControl", chkPaVolumeControl.IsChecked.ToString().ToLower());
            txtPaVolumeApp.IsEnabled = (bool)chkPaVolumeControl.IsChecked;
        }

        private void chkPaLatchMute_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("paLatchMute", chkPaLatchMute.IsChecked.ToString().ToLower());
        }

        private void txtPaVolumeApp_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter || e.Key != System.Windows.Input.Key.Return)
                return;

            txtPaVolumeApp_Set();
        }

        private void txtPaVolumeApp_LostFocus(object sender, RoutedEventArgs e)
        {
            txtPaVolumeApp_Set();
        }

        private void txtPaVolumeApp_Set()
        {
            if (txtPaVolumeApp?.Text != null)
                serviceModel.SetSetting("paVolumeApp", txtPaVolumeApp.Text);
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

        private void audioApi_Click(object sender, RoutedEventArgs e)
        {
            AudioApiType apiType = rbCoreAudio.IsChecked == true ?
                AudioApiType.CoreAudio : AudioApiType.VoiceMeeter;

            serviceModel.SetSetting("audioApiType", apiType.ToString());

            // Update UI visibility based on selected API
            UpdateAudioApiVisibility();

            // If VoiceMeeter is selected, load available strips
            if (apiType == AudioApiType.VoiceMeeter)
            {
                LoadVoiceMeeterStrips();
            }
        }

        private void UpdateAudioApiVisibility()
        {
            bool isCoreAudio = serviceModel.AudioApiType == AudioApiType.CoreAudio;

            // Update INT channel visibility
            pnlIntCoreAudio.Visibility = isCoreAudio ? Visibility.Visible : Visibility.Collapsed;
            pnlIntVoiceMeeter.Visibility = isCoreAudio ? Visibility.Collapsed : Visibility.Visible;

            // Update VHF1 channel visibility
            pnlVhf1CoreAudio.Visibility = isCoreAudio ? Visibility.Visible : Visibility.Collapsed;
            pnlVhf1VoiceMeeter.Visibility = isCoreAudio ? Visibility.Collapsed : Visibility.Visible;

            // Update VHF2 channel visibility
            pnlVhf2CoreAudio.Visibility = isCoreAudio ? Visibility.Visible : Visibility.Collapsed;
            pnlVhf2VoiceMeeter.Visibility = isCoreAudio ? Visibility.Collapsed : Visibility.Visible;
            
            // Update VHF3 channel visibility
            pnlVhf3CoreAudio.Visibility = isCoreAudio ? Visibility.Visible : Visibility.Collapsed;
            pnlVhf3VoiceMeeter.Visibility = isCoreAudio ? Visibility.Collapsed : Visibility.Visible;

            // Update CAB channel visibility
            pnlCabCoreAudio.Visibility = isCoreAudio ? Visibility.Visible : Visibility.Collapsed;
            pnlCabVoiceMeeter.Visibility = isCoreAudio ? Visibility.Collapsed : Visibility.Visible;

            // Update PA channel visibility
            pnlPaCoreAudio.Visibility = isCoreAudio ? Visibility.Visible : Visibility.Collapsed;
            pnlPaVoiceMeeter.Visibility = isCoreAudio ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LoadVoiceMeeterStrips()
        {
            try
            {
                // Get available strips from the AudioService
                var audioService = serviceModel.GetAudioService();
                if (audioService == null)
                    return;

                // Check if VoiceMeeter is running
                if (!audioService.IsVoiceMeeterRunning())
                {
                    // Show a message to the user
                    MessageBox.Show(
                        "VoiceMeeter is not running. Please start VoiceMeeter and click the Refresh button.",
                        "VoiceMeeter Not Running",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    // Try to start VoiceMeeter
                    if (audioService.EnsureVoiceMeeterIsRunning())
                    {
                        // Wait a moment for VoiceMeeter to initialize
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        // Clear the ComboBoxes
                        cboIntVoiceMeeterStrip.ItemsSource = null;
                        cboVhf1VoiceMeeterStrip.ItemsSource = null;
                        cboVhf2VoiceMeeterStrip.ItemsSource = null;
                        cboVhf3VoiceMeeterStrip.ItemsSource = null;
                        cboCabVoiceMeeterStrip.ItemsSource = null;
                        cboPaVoiceMeeterStrip.ItemsSource = null;

                        return;
                    }
                }

                // Set the radio buttons based on the device types
                _isLoadingSettings = true;

                if (serviceModel.VoiceMeeterDeviceTypes.TryGetValue(AudioChannel.INT, out var intDeviceType))
                {
                    rbIntVoiceMeeterStrip.IsChecked = intDeviceType == VoiceMeeterDeviceType.Strip;
                    rbIntVoiceMeeterBus.IsChecked = intDeviceType == VoiceMeeterDeviceType.Bus;
                }

                if (serviceModel.VoiceMeeterDeviceTypes.TryGetValue(AudioChannel.VHF1, out var vhf1DeviceType))
                {
                    rbVhf1VoiceMeeterStrip.IsChecked = vhf1DeviceType == VoiceMeeterDeviceType.Strip;
                    rbVhf1VoiceMeeterBus.IsChecked = vhf1DeviceType == VoiceMeeterDeviceType.Bus;
                }

                if (serviceModel.VoiceMeeterDeviceTypes.TryGetValue(AudioChannel.VHF2, out var vhf2DeviceType))
                {
                    rbVhf2VoiceMeeterStrip.IsChecked = vhf2DeviceType == VoiceMeeterDeviceType.Strip;
                    rbVhf2VoiceMeeterBus.IsChecked = vhf2DeviceType == VoiceMeeterDeviceType.Bus;
                }

                if (serviceModel.VoiceMeeterDeviceTypes.TryGetValue(AudioChannel.VHF3, out var vhf3DeviceType))
                {
                    rbVhf3VoiceMeeterStrip.IsChecked = vhf3DeviceType == VoiceMeeterDeviceType.Strip;
                    rbVhf3VoiceMeeterBus.IsChecked = vhf3DeviceType == VoiceMeeterDeviceType.Bus;
                }

                if (serviceModel.VoiceMeeterDeviceTypes.TryGetValue(AudioChannel.CAB, out var cabDeviceType))
                {
                    rbCabVoiceMeeterStrip.IsChecked = cabDeviceType == VoiceMeeterDeviceType.Strip;
                    rbCabVoiceMeeterBus.IsChecked = cabDeviceType == VoiceMeeterDeviceType.Bus;
                }

                if (serviceModel.VoiceMeeterDeviceTypes.TryGetValue(AudioChannel.PA, out var paDeviceType))
                {
                    rbPaVoiceMeeterStrip.IsChecked = paDeviceType == VoiceMeeterDeviceType.Strip;
                    rbPaVoiceMeeterBus.IsChecked = paDeviceType == VoiceMeeterDeviceType.Bus;
                }

                _isLoadingSettings = false;

                // Load the strips/buses for each channel
                LoadVoiceMeeterStripsForChannel(AudioChannel.INT);
                LoadVoiceMeeterStripsForChannel(AudioChannel.VHF1);
                LoadVoiceMeeterStripsForChannel(AudioChannel.VHF2);
                LoadVoiceMeeterStripsForChannel(AudioChannel.VHF3);
                LoadVoiceMeeterStripsForChannel(AudioChannel.CAB);
                LoadVoiceMeeterStripsForChannel(AudioChannel.PA);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "MainWindow", $"Error loading VoiceMeeter strips: {ex.Message}");
                MessageBox.Show(
                    $"Error loading VoiceMeeter strips: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void rbIntVoiceMeeterDeviceType_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoadingSettings)
                return;

            VoiceMeeterDeviceType deviceType = rbIntVoiceMeeterStrip.IsChecked == true ?
                VoiceMeeterDeviceType.Strip : VoiceMeeterDeviceType.Bus;

            serviceModel.SetVoiceMeeterDeviceType(AudioChannel.INT, deviceType);

            // Reload the ComboBox with the appropriate list
            LoadVoiceMeeterStripsForChannel(AudioChannel.INT);
        }

        private void rbVhf1VoiceMeeterDeviceType_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoadingSettings)
                return;

            VoiceMeeterDeviceType deviceType = rbVhf1VoiceMeeterStrip.IsChecked == true ?
                VoiceMeeterDeviceType.Strip : VoiceMeeterDeviceType.Bus;

            serviceModel.SetVoiceMeeterDeviceType(AudioChannel.VHF1, deviceType);

            // Reload the ComboBox with the appropriate list
            LoadVoiceMeeterStripsForChannel(AudioChannel.VHF1);
        }

        private void rbVhf2VoiceMeeterDeviceType_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoadingSettings)
                return;

            VoiceMeeterDeviceType deviceType = rbVhf2VoiceMeeterStrip.IsChecked == true ?
                VoiceMeeterDeviceType.Strip : VoiceMeeterDeviceType.Bus;

            serviceModel.SetVoiceMeeterDeviceType(AudioChannel.VHF2, deviceType);

            // Reload the ComboBox with the appropriate list
            LoadVoiceMeeterStripsForChannel(AudioChannel.VHF2);
        }

        private void rbVhf3VoiceMeeterDeviceType_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoadingSettings)
                return;

            VoiceMeeterDeviceType deviceType = rbVhf3VoiceMeeterStrip.IsChecked == true ?
                VoiceMeeterDeviceType.Strip : VoiceMeeterDeviceType.Bus;

            serviceModel.SetVoiceMeeterDeviceType(AudioChannel.VHF3, deviceType);

            // Reload the ComboBox with the appropriate list
            LoadVoiceMeeterStripsForChannel(AudioChannel.VHF3);
        }

        private void rbCabVoiceMeeterDeviceType_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoadingSettings)
                return;

            VoiceMeeterDeviceType deviceType = rbCabVoiceMeeterStrip.IsChecked == true ?
                VoiceMeeterDeviceType.Strip : VoiceMeeterDeviceType.Bus;

            serviceModel.SetVoiceMeeterDeviceType(AudioChannel.CAB, deviceType);

            // Reload the ComboBox with the appropriate list
            LoadVoiceMeeterStripsForChannel(AudioChannel.CAB);
        }

        private void rbPaVoiceMeeterDeviceType_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoadingSettings)
                return;

            VoiceMeeterDeviceType deviceType = rbPaVoiceMeeterStrip.IsChecked == true ?
                VoiceMeeterDeviceType.Strip : VoiceMeeterDeviceType.Bus;

            serviceModel.SetVoiceMeeterDeviceType(AudioChannel.PA, deviceType);

            // Reload the ComboBox with the appropriate list
            LoadVoiceMeeterStripsForChannel(AudioChannel.PA);
        }

        private void LoadVoiceMeeterStripsForChannel(AudioChannel channel)
        {
            try
            {
                // Get available strips/buses from the AudioService
                var audioService = serviceModel.GetAudioService();
                if (audioService == null)
                    return;

                // Get the device type
                VoiceMeeterDeviceType deviceType = VoiceMeeterDeviceType.Strip;
                if (serviceModel.VoiceMeeterDeviceTypes.TryGetValue(channel, out var type))
                {
                    deviceType = type;
                }

                // Get the appropriate list
                List<KeyValuePair<string, string>> devices;
                if (deviceType == VoiceMeeterDeviceType.Strip)
                {
                    devices = audioService.GetAvailableVoiceMeeterStrips();
                }
                else
                {
                    devices = audioService.GetAvailableVoiceMeeterBuses();
                }

                if (devices.Count == 0)
                {
                    return;
                }

                // Populate the ComboBox
                _isLoadingSettings = true;

                switch (channel)
                {
                    case AudioChannel.INT:
                        cboIntVoiceMeeterStrip.ItemsSource = devices;
                        if (serviceModel.VoiceMeeterStrips.TryGetValue(channel, out string intDevice) && !string.IsNullOrEmpty(intDevice))
                        {
                            cboIntVoiceMeeterStrip.SelectedValue = intDevice;
                        }
                        break;
                    case AudioChannel.VHF1:
                        cboVhf1VoiceMeeterStrip.ItemsSource = devices;
                        if (serviceModel.VoiceMeeterStrips.TryGetValue(channel, out string vhf1Device) && !string.IsNullOrEmpty(vhf1Device))
                        {
                            cboIntVoiceMeeterStrip.SelectedValue = vhf1Device;
                        }
                        break;
                    case AudioChannel.VHF2:
                        cboVhf2VoiceMeeterStrip.ItemsSource = devices;
                        if (serviceModel.VoiceMeeterStrips.TryGetValue(channel, out string vhf2Device) && !string.IsNullOrEmpty(vhf2Device))
                        {
                            cboIntVoiceMeeterStrip.SelectedValue = vhf2Device;
                        }
                        break;
                    case AudioChannel.VHF3:
                        cboVhf3VoiceMeeterStrip.ItemsSource = devices;
                        if (serviceModel.VoiceMeeterStrips.TryGetValue(channel, out string vhf3Device) && !string.IsNullOrEmpty(vhf3Device))
                        {
                            cboIntVoiceMeeterStrip.SelectedValue = vhf3Device;
                        }
                        break;
                    case AudioChannel.CAB:
                        cboCabVoiceMeeterStrip.ItemsSource = devices;
                        if (serviceModel.VoiceMeeterStrips.TryGetValue(channel, out string cabDevice) && !string.IsNullOrEmpty(cabDevice))
                        {
                            cboIntVoiceMeeterStrip.SelectedValue = cabDevice;
                        }
                        break;
                    case AudioChannel.PA:
                        cboPaVoiceMeeterStrip.ItemsSource = devices;
                        if (serviceModel.VoiceMeeterStrips.TryGetValue(channel, out string paDevice) && !string.IsNullOrEmpty(paDevice))
                        {
                            cboIntVoiceMeeterStrip.SelectedValue = paDevice;
                        }
                        break;
                }

                _isLoadingSettings = false;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "MainWindow", $"Error loading VoiceMeeter devices for channel {channel}: {ex.Message}");
            }
        }



        private void btnRefreshVoiceMeeterStrips_Click(object sender, RoutedEventArgs e)
        {
            LoadVoiceMeeterStrips();
        }

        private void cboIntVoiceMeeterStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingSettings)
                return;

            if (cboIntVoiceMeeterStrip.SelectedValue != null)
            {
                string stripName = cboIntVoiceMeeterStrip.SelectedValue.ToString();
                serviceModel.SetVoiceMeeterStrip(AudioChannel.INT, stripName);
            }
        }

        private void cboVhf1VoiceMeeterStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingSettings)
                return;

            if (cboVhf1VoiceMeeterStrip.SelectedValue != null)
            {
                string stripName = cboVhf1VoiceMeeterStrip.SelectedValue.ToString();
                serviceModel.SetVoiceMeeterStrip(AudioChannel.VHF1, stripName);
            }
        }
        private void cboVhf2VoiceMeeterStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingSettings)
                return;

            if (cboVhf2VoiceMeeterStrip.SelectedValue != null)
            {
                string stripName = cboVhf2VoiceMeeterStrip.SelectedValue.ToString();
                serviceModel.SetVoiceMeeterStrip(AudioChannel.VHF2, stripName);
            }
        }
        private void cboVhf3VoiceMeeterStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingSettings)
                return;

            if (cboVhf3VoiceMeeterStrip.SelectedValue != null)
            {
                string stripName = cboVhf3VoiceMeeterStrip.SelectedValue.ToString();
                serviceModel.SetVoiceMeeterStrip(AudioChannel.VHF3, stripName);
            }
        }
        private void cboCabVoiceMeeterStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingSettings)
                return;

            if (cboCabVoiceMeeterStrip.SelectedValue != null)
            {
                string stripName = cboCabVoiceMeeterStrip.SelectedValue.ToString();
                serviceModel.SetVoiceMeeterStrip(AudioChannel.CAB, stripName);
            }
        }

        private void cboPaVoiceMeeterStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingSettings)
                return;

            if (cboPaVoiceMeeterStrip.SelectedValue != null)
            {
                string stripName = cboPaVoiceMeeterStrip.SelectedValue.ToString();
                serviceModel.SetVoiceMeeterStrip(AudioChannel.PA, stripName);
            }
        }
    }
}
