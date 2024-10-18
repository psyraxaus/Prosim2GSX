﻿using System;
using System.Configuration;
using System.Globalization;

namespace Prosim2GSX
{
    public class ServiceModel
    {
        public bool ServiceExited { get; set; } = false;
        public bool CancellationRequested { get; set; } = false;
        public bool IsProsimRunning { get; set; } = false;
        public bool IsSimRunning { get; set; } = false;
        public bool IsSessionRunning { get; set; } = false;
        public string SimBriefURL { get; set; }
        public string SimBriefID { get; set; }
        public bool UseProsimEFB { get; set; }
        public string ProsimHostname { get; set; }
        public bool WaitForConnect { get; set; }
        public bool TestArrival { get; set; }
        public bool GsxVolumeControl { get; set; }
        public string Vhf1VolumeApp { get; set; }
        public bool Vhf1VolumeControl { get; set; }
        public bool Vhf1LatchMute { get; set; }
        public bool DisableCrew { get; set; }
        public bool RepositionPlane { get; set; }
        public float RepositionDelay { get; set; }
        public bool AutoConnect { get; set; }
        public float OperatorDelay { get; set; }
        public bool ConnectPCA { get; set; }
        public bool PcaOnlyJetways { get; set; }
        public bool AutoRefuel { get; set; }
        public bool CallCatering { get; set; }
        public bool AutoBoarding { get; set; }
        public bool AutoDeboarding { get; set; }
        public float RefuelRate { get; set; }
        public string RefuelUnit { get; set; }
        public bool SynchBypass { get; set; }
        public bool UseActualPaxValue { get; set; }

        protected Configuration AppConfiguration;

        public ServiceModel()
        {
            LoadConfiguration();
        }

        public bool IsVhf1Controllable()
        {
            return Vhf1VolumeControl && !string.IsNullOrEmpty(Vhf1VolumeApp);
        }

        protected void LoadConfiguration()
        {
            AppConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = AppConfiguration.AppSettings.Settings;

            SimBriefURL = Convert.ToString(settings["simbriefURL"].Value);
            SimBriefID = Convert.ToString(settings["pilotID"].Value);
            UseProsimEFB = Convert.ToBoolean(settings["useProsimEFB"].Value);
            ProsimHostname = Convert.ToString(settings["prosimHostname"].Value);
            WaitForConnect = Convert.ToBoolean(settings["waitForConnect"].Value);
            TestArrival = Convert.ToBoolean(settings["testArrival"].Value);
            GsxVolumeControl = Convert.ToBoolean(settings["gsxVolumeControl"].Value);
            Vhf1VolumeApp = Convert.ToString(settings["vhf1VolumeApp"].Value);
            Vhf1VolumeControl = Convert.ToBoolean(settings["vhf1VolumeControl"].Value);
            Vhf1LatchMute = Convert.ToBoolean(settings["vhf1LatchMute"].Value);
            DisableCrew = Convert.ToBoolean(settings["disableCrew"].Value);
            RepositionPlane = Convert.ToBoolean(settings["repositionPlane"].Value);
            RepositionDelay = Convert.ToSingle(settings["repositionDelay"].Value, CultureInfo.InvariantCulture);
            AutoConnect = Convert.ToBoolean(settings["autoConnect"].Value);
            OperatorDelay = Convert.ToSingle(settings["operatorDelay"].Value, CultureInfo.InvariantCulture);
            ConnectPCA = Convert.ToBoolean(settings["connectPCA"].Value);
            PcaOnlyJetways = Convert.ToBoolean(settings["pcaOnlyJetway"].Value);
            AutoRefuel = Convert.ToBoolean(settings["autoRefuel"].Value);
            CallCatering = Convert.ToBoolean(settings["callCatering"].Value);
            AutoBoarding = Convert.ToBoolean(settings["autoBoarding"].Value);
            AutoDeboarding = Convert.ToBoolean(settings["autoDeboarding"].Value);
            RefuelRate = Convert.ToSingle(settings["refuelRate"].Value, CultureInfo.InvariantCulture);
            RefuelUnit = Convert.ToString(settings["refuelUnit"].Value);
            SynchBypass = Convert.ToBoolean(settings["synchBypass"].Value);
            UseActualPaxValue = Convert.ToBoolean(settings["useActualValue"].Value);
        }

        protected void SaveConfiguration()
        {
            AppConfiguration.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(AppConfiguration.AppSettings.SectionInformation.Name);
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            return AppConfiguration.AppSettings.Settings[key].Value ?? defaultValue;
        }

        public void SetSetting(string key, string value)
        {
            if (AppConfiguration.AppSettings.Settings[key] != null)
            {
                AppConfiguration.AppSettings.Settings[key].Value = value;
                SaveConfiguration();
                LoadConfiguration();
            }
        }

        public float GetFuelRateKGS()
        {
            if (RefuelUnit == "KGS")
                return RefuelRate;
            else
                return RefuelRate / ProsimController.weightConversion;
        }
    }
}