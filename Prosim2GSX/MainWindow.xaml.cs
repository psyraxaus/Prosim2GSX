﻿using System;
using System.Globalization;
using System.Windows;
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
            chkAutoDeboard.IsChecked = serviceModel.AutoDeboarding;
            chkAutoRefuel.IsChecked = serviceModel.AutoRefuel;
            chkAutoReposition.IsChecked = serviceModel.RepositionPlane;
            chkCallCatering.IsChecked = serviceModel.CallCatering;
            chkConnectPCA.IsChecked = serviceModel.ConnectPCA;
            chkDisableCrewBoarding.IsChecked = serviceModel.DisableCrew;
            chkGsxVolumeControl.IsChecked = serviceModel.GsxVolumeControl;
            chkPcaOnlyJetway.IsChecked = serviceModel.PcaOnlyJetways;
            chkSynchBypass.IsChecked = serviceModel.SynchBypass;
            chkSaveFuel.IsChecked = serviceModel.SetSaveFuel;
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

        private void chkAutoBoard_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("autoBoarding", chkAutoBoard.IsChecked.ToString().ToLower());
        }

        private void chkAutoConnect_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("autoConnect", chkAutoConnect.IsChecked.ToString().ToLower());
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

        private void chkGsxVolumeControl_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("gsxVolumeControl", chkGsxVolumeControl.IsChecked.ToString().ToLower());
        }

        private void chkOpenDoorCatering_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("setOpenAftDoorCatering", chkOpenDoorCatering.IsChecked.ToString().ToLower());
        }
        
        private void chkPcaOnlyJetway_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("pcaOnlyJetway", chkPcaOnlyJetway.IsChecked.ToString().ToLower());
        }

        private void chkSaveFuel_Checked(object sender, RoutedEventArgs e)
        {
            chkZeroFuel.IsEnabled = false;
            serviceModel.SetSetting("setSaveFuel", chkSaveFuel.IsChecked.ToString().ToLower());
        }

        private void chkSaveFuel_unChecked(object sender, RoutedEventArgs e)
        {
            chkZeroFuel.IsEnabled = true;
            serviceModel.SetSetting("setSaveFuel", chkSaveFuel.IsChecked.ToString().ToLower());
        }

        private void chkSynchBypass_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("synchBypass", chkSynchBypass.IsChecked.ToString().ToLower());
        }

        private void chkUseActualPaxValue_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("useActualValue", chkUseActualPaxValue.IsChecked.ToString().ToLower());
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

        private void txtRepositionDelay_LostFocus(object sender, RoutedEventArgs e)
        {
            txtRepositionDelay_Set();
        }

        private void txtRepositionDelay_Set()
        {
            if (float.TryParse(txtRepositionDelay.Text, CultureInfo.InvariantCulture, out _))
                serviceModel.SetSetting("repositionDelay", Convert.ToString(txtRepositionDelay.Text, CultureInfo.InvariantCulture));
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
            if (serviceModel.IsSimRunning)
                lblConnStatMSFS.Foreground = new SolidColorBrush(Colors.DarkGreen);
            else
                lblConnStatMSFS.Foreground = new SolidColorBrush(Colors.Red);

            if (IPCManager.SimConnect != null && IPCManager.SimConnect.IsReady)
                lblConnStatSimConnect.Foreground = new SolidColorBrush(Colors.DarkGreen);
            else
                lblConnStatSimConnect.Foreground = new SolidColorBrush(Colors.Red);

            if (serviceModel.IsProsimRunning)
                lblConnStatProsim.Foreground = new SolidColorBrush(Colors.DarkGreen);
            else
                lblConnStatProsim.Foreground = new SolidColorBrush(Colors.Red);

            if (serviceModel.IsSessionRunning)
                lblConnStatSession.Foreground = new SolidColorBrush(Colors.DarkGreen);
            else
                lblConnStatSession.Foreground = new SolidColorBrush(Colors.Red);
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



    }
}
