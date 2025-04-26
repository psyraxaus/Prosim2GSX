using Prosim2GSX.Behaviours;
using Prosim2GSX.Services;
using Prosim2GSX.Services.Audio;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using static Prosim2GSX.Services.Audio.AudioChannelConfig;

namespace Prosim2GSX.Models
{
    public class ServiceModel
    {
        private AudioService _audioService;
        public string AcarsNetwork {  get; set; }
        public string AcarsNetworkUrl { get; set; }
        public string AcarsSecret { get; set; }
        public AudioApiType AudioApiType { get; set; }
        public bool AutoBoarding { get; set; }
        public bool AutoConnect { get; set; }
        public bool JetwayOnly { get; set; }
        public bool AutoDeboarding { get; set; }
        public bool AutoRefuel { get; set; }
        public bool CallCatering { get; set; }
        public bool CancellationRequested { get; set; } = false;
        public bool ConnectPCA { get; set; }
        public bool DisableCrew { get; set; }
        public string FlightPlanType { get; set; }
        public bool GsxVolumeControl { get; set; }
        public double HydaulicsBlueAmount { get; set; }
        public double HydaulicsGreenAmount { get; set; }
        public double HydaulicsYellowAmount { get; set; }
        public string IntVolumeApp { get; set; }
        public bool IntLatchMute { get; set; }
        public bool IsProsimRunning { get; set; } = false;
        public bool IsSessionRunning { get; set; } = false;
        public bool IsSimRunning { get; set; } = false;
        public string LogFilePath { get; set; }
        public string LogLevel { get; set; }
        public float OperatorDelay { get; set; }
        public bool PcaOnlyJetways { get; set; }
        public string ProsimHostname { get; set; }
        public float RefuelRate { get; set; }
        public string RefuelUnit { get; set; }
        public float RepositionDelay { get; set; }
        public bool RepositionPlane { get; set; }
        public double SavedFuelAmount { get; set; }
        public bool ServiceExited { get; set; } = false;
        public bool SetOpenCateringDoor { get; set; }
        public bool SetOpenCargoDoors { get; set; }
        public bool SetSaveHydraulicFluids { get; set; }
        public bool SetSaveFuel { get; set; }
        public bool SetZeroFuel { get; set; }
        public string SimBriefID { get; set; }
        public string SimBriefURL { get; set; }
        public bool SynchBypass { get; set; }
        public bool TestArrival { get; set; }
        public bool UseAcars { get; set; }
        public bool UseActualPaxValue { get; set; }
        public bool WaitForConnect { get; set; }
        public bool Vhf1LatchMute { get; set; }
        public string Vhf1VolumeApp { get; set; }
        public bool Vhf1VolumeControl { get; set; }
        public bool Vhf2VolumeControl { get; set; }
        public string Vhf2VolumeApp { get; set; }
        public bool Vhf2LatchMute { get; set; }

        public bool Vhf3VolumeControl { get; set; }
        public string Vhf3VolumeApp { get; set; }
        public bool Vhf3LatchMute { get; set; }

        public bool CabVolumeControl { get; set; }
        public string CabVolumeApp { get; set; }
        public bool CabLatchMute { get; set; }

        public bool PaVolumeControl { get; set; }
        public string PaVolumeApp { get; set; }
        public bool PaLatchMute { get; set; }

        protected ConfigurationFile ConfigurationFile = new();

        public Dictionary<AudioChannel, AudioChannelConfig> AudioChannels { get; private set; } = new Dictionary<AudioChannel, AudioChannelConfig>();

        public Dictionary<AudioChannel, string> VoiceMeeterStrips { get; private set; } = new Dictionary<AudioChannel, string>();

        public Dictionary<AudioChannel, VoiceMeeterDeviceType> VoiceMeeterDeviceTypes { get; private set; } = new Dictionary<AudioChannel, VoiceMeeterDeviceType>();

        public Dictionary<AudioChannel, string> VoiceMeeterStripLabels { get; private set; } = new Dictionary<AudioChannel, string>();

        /// <summary>
        /// Path to the ProsimSDK.dll file
        /// </summary>
        public string ProsimSDKPath { get; set; } = string.Empty;

        /// <summary>
        /// Path to the VoicemeeterRemote64.dll file
        /// </summary>
        public string VoicemeeterDllPath { get; set; } = string.Empty;

        /// <summary>
        /// Whether external dependencies have been configured
        /// </summary>
        public bool ExternalDependenciesConfigured { get; set; } = false;

        public ServiceModel()
        {
            LoadConfiguration();
        }

        public bool IsVhf1Controllable()
        {
            bool hasVoiceMeeterStrip = AudioApiType == AudioApiType.VoiceMeeter && 
                                       VoiceMeeterStrips.ContainsKey(AudioChannel.VHF1) && 
                                       !string.IsNullOrEmpty(VoiceMeeterStrips[AudioChannel.VHF1]);
            
            bool hasCoreAudioApp = !string.IsNullOrEmpty(Vhf1VolumeApp);
            
            // Log the result for debugging
            LogService.Log(Services.Logger.Enums.LogLevel.Debug, "ServiceModel", 
                $"IsVhf1Controllable: Control={Vhf1VolumeControl}, API={AudioApiType}, " +
                $"HasStrip={hasVoiceMeeterStrip}, HasApp={hasCoreAudioApp}, " +
                $"Result={Vhf1VolumeControl && (hasVoiceMeeterStrip || hasCoreAudioApp)}");
            
            return Vhf1VolumeControl && (hasVoiceMeeterStrip || hasCoreAudioApp);
        }

        public bool IsVhf2Controllable()
        {
            bool hasVoiceMeeterStrip = AudioApiType == AudioApiType.VoiceMeeter && 
                                       VoiceMeeterStrips.ContainsKey(AudioChannel.VHF2) && 
                                       !string.IsNullOrEmpty(VoiceMeeterStrips[AudioChannel.VHF2]);
            
            bool hasCoreAudioApp = !string.IsNullOrEmpty(Vhf2VolumeApp);
            
            // Log the result for debugging
            LogService.Log(Services.Logger.Enums.LogLevel.Debug, "ServiceModel", 
                $"IsVhf2Controllable: Control={Vhf2VolumeControl}, API={AudioApiType}, " +
                $"HasStrip={hasVoiceMeeterStrip}, HasApp={hasCoreAudioApp}, " +
                $"Result={Vhf2VolumeControl && (hasVoiceMeeterStrip || hasCoreAudioApp)}");
            
            return Vhf2VolumeControl && (hasVoiceMeeterStrip || hasCoreAudioApp);
        }

        public bool IsVhf3Controllable()
        {
            bool hasVoiceMeeterStrip = AudioApiType == AudioApiType.VoiceMeeter && 
                                       VoiceMeeterStrips.ContainsKey(AudioChannel.VHF3) && 
                                       !string.IsNullOrEmpty(VoiceMeeterStrips[AudioChannel.VHF3]);
            
            bool hasCoreAudioApp = !string.IsNullOrEmpty(Vhf3VolumeApp);
            
            // Log the result for debugging
            LogService.Log(Services.Logger.Enums.LogLevel.Debug, "ServiceModel", 
                $"IsVhf3Controllable: Control={Vhf3VolumeControl}, API={AudioApiType}, " +
                $"HasStrip={hasVoiceMeeterStrip}, HasApp={hasCoreAudioApp}, " +
                $"Result={Vhf3VolumeControl && (hasVoiceMeeterStrip || hasCoreAudioApp)}");
            
            return Vhf3VolumeControl && (hasVoiceMeeterStrip || hasCoreAudioApp);
        }

        public bool IsCabControllable()
        {
            bool hasVoiceMeeterStrip = AudioApiType == AudioApiType.VoiceMeeter && 
                                       VoiceMeeterStrips.ContainsKey(AudioChannel.CAB) && 
                                       !string.IsNullOrEmpty(VoiceMeeterStrips[AudioChannel.CAB]);
            
            bool hasCoreAudioApp = !string.IsNullOrEmpty(CabVolumeApp);
            
            // Log the result for debugging
            LogService.Log(Services.Logger.Enums.LogLevel.Debug, "ServiceModel", 
                $"IsCabControllable: Control={CabVolumeControl}, API={AudioApiType}, " +
                $"HasStrip={hasVoiceMeeterStrip}, HasApp={hasCoreAudioApp}, " +
                $"Result={CabVolumeControl && (hasVoiceMeeterStrip || hasCoreAudioApp)}");
            
            return CabVolumeControl && (hasVoiceMeeterStrip || hasCoreAudioApp);
        }

        public bool IsPaControllable()
        {
            bool hasVoiceMeeterStrip = AudioApiType == AudioApiType.VoiceMeeter && 
                                       VoiceMeeterStrips.ContainsKey(AudioChannel.PA) && 
                                       !string.IsNullOrEmpty(VoiceMeeterStrips[AudioChannel.PA]);
            
            bool hasCoreAudioApp = !string.IsNullOrEmpty(PaVolumeApp);
            
            // Log the result for debugging
            LogService.Log(Services.Logger.Enums.LogLevel.Debug, "ServiceModel", 
                $"IsPaControllable: Control={PaVolumeControl}, API={AudioApiType}, " +
                $"HasStrip={hasVoiceMeeterStrip}, HasApp={hasCoreAudioApp}, " +
                $"Result={PaVolumeControl && (hasVoiceMeeterStrip || hasCoreAudioApp)}");
            
            return PaVolumeControl && (hasVoiceMeeterStrip || hasCoreAudioApp);
        }

        protected void LoadConfiguration()
        {
            ConfigurationFile.LoadConfiguration();

            AcarsNetwork = Convert.ToString(ConfigurationFile.GetSetting("acarsNetwork", "Hoppie"));
            AcarsNetworkUrl = Convert.ToString(ConfigurationFile.GetSetting("acarsNetworkUrl", "http://www.hoppie.nl/acars/system/connect.html"));
            AcarsSecret = Convert.ToString(ConfigurationFile.GetSetting("acarsSecret", ""));
            AudioApiType = (AudioApiType)Enum.Parse(typeof(AudioApiType),Convert.ToString(ConfigurationFile.GetSetting("audioApiType", "CoreAudio")));
            AutoBoarding = Convert.ToBoolean(ConfigurationFile.GetSetting("autoBoarding", "true"));
            AutoConnect = Convert.ToBoolean(ConfigurationFile.GetSetting("autoConnect", "true"));
            JetwayOnly = Convert.ToBoolean(ConfigurationFile.GetSetting("jetwayOnly", "false"));
            AutoDeboarding = Convert.ToBoolean(ConfigurationFile.GetSetting("autoDeboarding", "true"));
            AutoRefuel = Convert.ToBoolean(ConfigurationFile.GetSetting("autoRefuel", "true"));
            CabVolumeApp = Convert.ToString(ConfigurationFile.GetSetting("cabVolumeApp", ""));
            CabVolumeControl = Convert.ToBoolean(ConfigurationFile.GetSetting("cabVolumeControl", "false"));
            CabLatchMute = Convert.ToBoolean(ConfigurationFile.GetSetting("cabLatchMute", "true"));
            CallCatering = Convert.ToBoolean(ConfigurationFile.GetSetting("callCatering", "true"));
            ConnectPCA = Convert.ToBoolean(ConfigurationFile.GetSetting("connectPCA", "true"));
            DisableCrew = Convert.ToBoolean(ConfigurationFile.GetSetting("disableCrew", "true"));
            FlightPlanType = Convert.ToString(ConfigurationFile.GetSetting("flightPlanType", "MCDU"));
            GsxVolumeControl = Convert.ToBoolean(ConfigurationFile.GetSetting("gsxVolumeControl", "true"));
            HydaulicsBlueAmount = Convert.ToSingle(ConfigurationFile.GetSetting("hydraulicBlueAmount", "0"), new RealInvariantFormat(ConfigurationFile.GetSetting("hydraulicBlueAmount", "0")));
            HydaulicsGreenAmount = Convert.ToSingle(ConfigurationFile.GetSetting("hydraulicGreenAmount", "0"), new RealInvariantFormat(ConfigurationFile.GetSetting("hydraulicGreenAmount", "0")));
            HydaulicsYellowAmount = Convert.ToSingle(ConfigurationFile.GetSetting("hydraulicYellowAmount", "0"), new RealInvariantFormat(ConfigurationFile.GetSetting("hydraulicYellowAmount", "0")));
            IntVolumeApp = Convert.ToString(ConfigurationFile.GetSetting("intVolumeApp", "Couatl64_MSFS"));
            IntLatchMute = Convert.ToBoolean(ConfigurationFile.GetSetting("intLatchMute", "true"));
            LogFilePath = Convert.ToString(ConfigurationFile.GetSetting("logFilePath", "Prosim2GSX.log"));
            LogLevel = Convert.ToString(ConfigurationFile.GetSetting("logLevel", "Debug"));
            PcaOnlyJetways = Convert.ToBoolean(ConfigurationFile.GetSetting("pcaOnlyJetway", "true"));
            PaVolumeApp = Convert.ToString(ConfigurationFile.GetSetting("paVolumeApp", ""));
            PaVolumeControl = Convert.ToBoolean(ConfigurationFile.GetSetting("paVolumeControl", "false"));
            PaLatchMute = Convert.ToBoolean(ConfigurationFile.GetSetting("paLatchMute", "true"));
            RefuelRate = Convert.ToSingle(ConfigurationFile.GetSetting("refuelRate", "28"), new RealInvariantFormat(ConfigurationFile.GetSetting("refuelRate", "28")));
            RefuelUnit = Convert.ToString(ConfigurationFile.GetSetting("refuelUnit", "KGS"));
            ProsimHostname = Convert.ToString(ConfigurationFile.GetSetting("prosimHostname", "127.0.0.1"));
            RepositionDelay = Convert.ToSingle(ConfigurationFile.GetSetting("repositionDelay", "3"), new RealInvariantFormat(ConfigurationFile.GetSetting("repositionDelay", "3")));
            RepositionPlane = Convert.ToBoolean(ConfigurationFile.GetSetting("repositionPlane", "true"));
            OperatorDelay = Convert.ToSingle(ConfigurationFile.GetSetting("operatorDelay", "10"), new RealInvariantFormat(ConfigurationFile.GetSetting("operatorDelay", "10")));
            SavedFuelAmount = Convert.ToSingle(ConfigurationFile.GetSetting("savedFuelAmount", "0"), new RealInvariantFormat(ConfigurationFile.GetSetting("savedFuelAmount", "0")));
            SetOpenCateringDoor = Convert.ToBoolean(ConfigurationFile.GetSetting("setOpenAftDoorCatering", "false"));
            SetOpenCargoDoors = Convert.ToBoolean(ConfigurationFile.GetSetting("setOpenCargoDoors", "true"));
            SetSaveHydraulicFluids = Convert.ToBoolean(ConfigurationFile.GetSetting("saveHydraulicFluids", "false"));
            SetSaveFuel = Convert.ToBoolean(ConfigurationFile.GetSetting("setSaveFuel", "false"));
            SetZeroFuel = Convert.ToBoolean(ConfigurationFile.GetSetting("setZeroFuel", "false"));
            SimBriefID = Convert.ToString(ConfigurationFile.GetSetting("pilotID", "0"));
            SimBriefURL = Convert.ToString(ConfigurationFile.GetSetting("simbriefURL", "https://www.simbrief.com/api/xml.fetcher.php?userid={0}"));
            SynchBypass = Convert.ToBoolean(ConfigurationFile.GetSetting("synchBypass", "true"));
            TestArrival = Convert.ToBoolean(ConfigurationFile.GetSetting("testArrival", "false"));
            UseAcars = Convert.ToBoolean(ConfigurationFile.GetSetting("useAcars", "false"));
            UseActualPaxValue = Convert.ToBoolean(ConfigurationFile.GetSetting("useActualValue", "true"));
            Vhf1VolumeApp = Convert.ToString(ConfigurationFile.GetSetting("vhf1VolumeApp", "vPilot"));
            Vhf1VolumeControl = Convert.ToBoolean(ConfigurationFile.GetSetting("vhf1VolumeControl", "false"));
            Vhf1LatchMute = Convert.ToBoolean(ConfigurationFile.GetSetting("vhf1LatchMute", "true"));
            Vhf2VolumeApp = Convert.ToString(ConfigurationFile.GetSetting("vhf2VolumeApp", ""));
            Vhf2VolumeControl = Convert.ToBoolean(ConfigurationFile.GetSetting("vhf2VolumeControl", "false"));
            Vhf2LatchMute = Convert.ToBoolean(ConfigurationFile.GetSetting("vhf2LatchMute", "true"));
            Vhf3VolumeApp = Convert.ToString(ConfigurationFile.GetSetting("vhf3VolumeApp", ""));
            Vhf3VolumeControl = Convert.ToBoolean(ConfigurationFile.GetSetting("vhf3VolumeControl", "false"));
            Vhf3LatchMute = Convert.ToBoolean(ConfigurationFile.GetSetting("vhf3LatchMute", "true"));
            WaitForConnect = Convert.ToBoolean(ConfigurationFile.GetSetting("waitForConnect", "true"));
            ProsimSDKPath = Convert.ToString(ConfigurationFile.GetSetting("prosimSDKPath", ""));
            VoicemeeterDllPath = Convert.ToString(ConfigurationFile.GetSetting("voicemeeterDllPath", ""));
            ExternalDependenciesConfigured = Convert.ToBoolean(ConfigurationFile.GetSetting("externalDependenciesConfigured", "false"));

            foreach (var channel in Enum.GetValues(typeof(AudioChannel)).Cast<AudioChannel>())
            {
                string stripLabel = Convert.ToString(ConfigurationFile.GetSetting($"voiceMeeter{channel}StripLabel", ""));
                VoiceMeeterStripLabels[channel] = stripLabel;
            }

            foreach (var channel in Enum.GetValues(typeof(AudioChannel)).Cast<AudioChannel>())
            {
                string deviceTypeStr = Convert.ToString(ConfigurationFile.GetSetting($"voiceMeeter{channel}DeviceType", "Strip"));
                VoiceMeeterDeviceTypes[channel] = Enum.TryParse<VoiceMeeterDeviceType>(deviceTypeStr, out var deviceType) ?
                    deviceType : VoiceMeeterDeviceType.Strip;
            }

            foreach (var channel in Enum.GetValues(typeof(AudioChannel)).Cast<AudioChannel>())
            {
                string stripName = Convert.ToString(ConfigurationFile.GetSetting($"voiceMeeter{channel}Strip", ""));
                if (!string.IsNullOrEmpty(stripName))
                {
                    VoiceMeeterStrips[channel] = stripName;
                }
            }

            InitializeAudioChannels();
        }

        private void InitializeAudioChannels()
        {
            // INT
            AudioChannels[AudioChannel.INT] = new AudioChannelConfig
            {
                ProcessName = IntVolumeApp,
                VolumeDataRef = "system.analog.A_ASP_INT_VOLUME",
                MuteDataRef = "system.indicators.I_ASP_INT_REC",
                Enabled = GsxVolumeControl,
                LatchMute = IntLatchMute,
                VoiceMeeterStrip = VoiceMeeterStrips.ContainsKey(AudioChannel.INT) ? VoiceMeeterStrips[AudioChannel.INT] : ""
            };

            // VHF1
            AudioChannels[AudioChannel.VHF1] = new AudioChannelConfig
            {
                ProcessName = Vhf1VolumeApp,
                VolumeDataRef = "system.analog.A_ASP_VHF_1_VOLUME",
                MuteDataRef = "system.indicators.I_ASP_VHF_1_REC",
                Enabled = IsVhf1Controllable(),
                LatchMute = Vhf1LatchMute,
                VoiceMeeterStrip = VoiceMeeterStrips.ContainsKey(AudioChannel.VHF1) ? VoiceMeeterStrips[AudioChannel.VHF1] : ""
            };

            // VHF2
            AudioChannels[AudioChannel.VHF2] = new AudioChannelConfig
            {
                ProcessName = Vhf2VolumeApp,
                VolumeDataRef = "system.analog.A_ASP_VHF_2_VOLUME",
                MuteDataRef = "system.indicators.I_ASP_VHF_2_REC",
                Enabled = IsVhf2Controllable(),
                LatchMute = Vhf2LatchMute,
                VoiceMeeterStrip = VoiceMeeterStrips.ContainsKey(AudioChannel.VHF2) ? VoiceMeeterStrips[AudioChannel.VHF2] : ""
            };

            // VHF3
            AudioChannels[AudioChannel.VHF3] = new AudioChannelConfig
            {
                ProcessName = Vhf3VolumeApp,
                VolumeDataRef = "system.analog.A_ASP_VHF_3_VOLUME",
                MuteDataRef = "system.indicators.I_ASP_VHF_3_REC",
                Enabled = IsVhf3Controllable(),
                LatchMute = Vhf3LatchMute,
                VoiceMeeterStrip = VoiceMeeterStrips.ContainsKey(AudioChannel.VHF3) ? VoiceMeeterStrips[AudioChannel.VHF3] : ""
            };

            // CAB
            AudioChannels[AudioChannel.CAB] = new AudioChannelConfig
            {
                ProcessName = CabVolumeApp,
                VolumeDataRef = "system.analog.A_ASP_CAB_VOLUME",
                MuteDataRef = "system.indicators.I_ASP_CAB_REC",
                Enabled = IsCabControllable(),
                LatchMute = CabLatchMute,
                VoiceMeeterStrip = VoiceMeeterStrips.ContainsKey(AudioChannel.CAB) ? VoiceMeeterStrips[AudioChannel.CAB] : ""
            };

            // PA
            AudioChannels[AudioChannel.PA] = new AudioChannelConfig
            {
                ProcessName = PaVolumeApp,
                VolumeDataRef = "system.analog.A_ASP_PA_VOLUME",
                MuteDataRef = "system.indicators.I_ASP_PA_REC",
                Enabled = IsPaControllable(),
                LatchMute = PaLatchMute,
                VoiceMeeterStrip = VoiceMeeterStrips.ContainsKey(AudioChannel.PA) ? VoiceMeeterStrips[AudioChannel.PA] : ""
            };
        }

        public string GetSetting(string key, string defaultValue = "")
        {
            return ConfigurationFile[key] ?? defaultValue;
        }

        public void SetSetting(string key, string value, bool noLoad = false)
        {
            ConfigurationFile[key] = value;
            if (!noLoad)
                LoadConfiguration();
        }

        /// <summary>
        /// Gets the fuel rate in kilograms per second
        /// </summary>
        /// <returns>Fuel rate in kg/s</returns>
        public float GetFuelRateKGS()
        {
            // The weight conversion factor (2.205 lbs per kg)
            const float WeightConversion = 2.205f;

            // Get the current weight units from config
            string units = RefuelUnit ?? "KG"; // Default to KG if not set

            // If units are in KG, return the rate directly
            if (units.Equals("KG", StringComparison.OrdinalIgnoreCase))
            {
                return RefuelRate; // Return the rate directly in kg/s
            }
            // If units are in LBS, convert to KG
            else if (units.Equals("LBS", StringComparison.OrdinalIgnoreCase))
            {
                // Convert from lbs/s to kg/s
                float rateInKgPerSecond = RefuelRate / WeightConversion;

                LogService.Log(Services.Logger.Enums.LogLevel.Debug, nameof(ServiceModel),
                    $"Converting fuel rate from LBS: {RefuelRate} lbs/s = {rateInKgPerSecond} kg/s");

                return rateInKgPerSecond;
            }

            // Default fallback
            return RefuelRate;
        }

        public void SetVoiceMeeterStrip(AudioChannel channel, string stripName, string stripLabel)
        {
            VoiceMeeterStrips[channel] = stripName;
            VoiceMeeterStripLabels[channel] = stripLabel;
            SetSetting($"voiceMeeter{channel}Strip", stripName);
            SetSetting($"voiceMeeter{channel}StripLabel", stripLabel);
        }
        public void SetAudioService(AudioService audioService)
        {
            _audioService = audioService;
        }

        public AudioService GetAudioService()
        {
            return _audioService;
        }

        public void SetVoiceMeeterDeviceType(AudioChannel channel, VoiceMeeterDeviceType deviceType)
        {
            VoiceMeeterDeviceTypes[channel] = deviceType;
            SetSetting($"voiceMeeter{channel}DeviceType", deviceType.ToString());
        }

        public bool IsValidSimbriefId()
        {
            return !string.IsNullOrWhiteSpace(SimBriefID) && 
                   SimBriefID != "0" && 
                   int.TryParse(SimBriefID, out _); // Ensure it's a valid number
        }


    }
}
