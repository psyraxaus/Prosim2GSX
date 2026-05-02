// Wire-contract types. Mirror the C# DTOs in Prosim2GSX/Web/Contracts/
// (and the enum value names from the CFIT/Prosim source). Wire format
// rules are locked in project memory project_web_json_contract_decisions:
//   - camelCase property names
//   - enum values as string literals (declared C# names)
//   - TimeSpan as total seconds (number)
//   - dictionaries with enum keys → object with string-name keys
//   - dictionaries with int order keys → arrays

// ──────────────────────────────────────────────────────────────────────────
// WebSocket envelopes
// ──────────────────────────────────────────────────────────────────────────

export type ConnectionStatus = "connecting" | "open" | "reconnecting" | "closed";

export type WsChannel = "flightStatus" | "gsx" | "audio" | "appSettings" | "ofp" | "checklists";

export type StateChannel = "flightStatus" | "audio" | "gsxSettings" | "appSettings" | "ofp" | "checklists";

export interface PatchEnvelope {
  channel: WsChannel;
  patch: Record<string, unknown>;
}

export interface SnapshotEnvelope {
  channel: WsChannel;
  snapshot: Record<string, unknown>;
}

export interface LogAddedEnvelope {
  channel: "flightStatus";
  logAdded: string;
}

export type WsEnvelope = PatchEnvelope | SnapshotEnvelope | LogAddedEnvelope;

// ──────────────────────────────────────────────────────────────────────────
// Enums (string-literal unions matching C# enum value names)
// ──────────────────────────────────────────────────────────────────────────

export type GsxServiceState =
  | "Unknown"
  | "Callable"
  | "NotAvailable"
  | "Bypassed"
  | "Requested"
  | "Active"
  | "Completed";

export type GsxServiceType =
  | "Unknown"
  | "Reposition"
  | "Refuel"
  | "Catering"
  | "Boarding"
  | "Pushback"
  | "Deice"
  | "Deboarding"
  | "GPU"
  | "Water"
  | "Lavatory"
  | "Jetway"
  | "Stairs"
  | "Cleaning";

export type GsxServiceActivation =
  | "Skip"
  | "Manual"
  | "AfterCalled"
  | "AfterRequested"
  | "AfterActive"
  | "AfterPrevCompleted"
  | "AfterAllCompleted";

export type GsxServiceConstraint =
  | "NoneAlways"
  | "FirstLeg"
  | "TurnAround"
  | "CompanyHub"
  | "NonCompanyHub";

export type GsxMenuState = "UNKNOWN" | "READY" | "HIDE" | "TIMEOUT" | "DISABLED";

export type AutomationState =
  | "SessionStart"
  | "Preparation"
  | "Departure"
  | "PushBack"
  | "TaxiOut"
  | "Flight"
  | "TaxiIn"
  | "Arrival"
  | "TurnAround";

export type AudioChannel = "VHF1" | "VHF2" | "VHF3" | "HF1" | "HF2" | "INT" | "CAB" | "PA";
export type AcpSide = "CPT" | "FO";
export type DataFlow = "Render" | "Capture" | "All";
export type DeviceState = "Active" | "Disabled" | "NotPresent" | "Unplugged" | "MaskAll";

export type DisplayUnit = "KG" | "LB";
export type DisplayUnitSource = "App" | "Aircraft";

export type RefuelMethod = "FixedRate" | "DynamicRate";

export type AutoDeiceFluid =
  | "TypeI100"
  | "TypeI75"
  | "TypeII100"
  | "TypeII75"
  | "TypeIV100"
  | "TypeIV75";

export type PushbackPreference = "Straight" | "TailLeft" | "TailRight";

export type ProfileMatchType = "Default" | "Airline" | "Title";

// ──────────────────────────────────────────────────────────────────────────
// FlightStatusDto + GsxLiveDto (read-only — Monitor tab)
// ──────────────────────────────────────────────────────────────────────────

export interface GsxLiveDto {
  gsxRunning: boolean;
  gsxStarted: string;
  gsxStartedValid: boolean;
  gsxMenu: GsxMenuState;
  gsxPaxTarget: number;
  gsxPaxTotal: string;
  gsxCargoProgress: string;

  serviceReposition: GsxServiceState;
  serviceRefuel: GsxServiceState;
  serviceCatering: GsxServiceState;
  serviceLavatory: GsxServiceState;
  serviceWater: GsxServiceState;
  serviceCleaning: GsxServiceState;

  serviceGpuConnected: boolean;
  serviceGpuPhaseRelevant: boolean;

  serviceBoarding: GsxServiceState;
  serviceDeboarding: GsxServiceState;
  servicePushback: string;
  serviceJetway: GsxServiceState;
  serviceJetwayConnected: boolean;
  serviceStairs: GsxServiceState;
  serviceStairsConnected: boolean;

  appAutomationState: AutomationState;
  appAutomationDepartureServices: string;

  assignedArrivalGate: string;
}

export interface FlightStatusDto {
  simRunning: boolean;
  simConnected: boolean;
  simSession: boolean;
  simPaused: boolean;
  simWalkaround: boolean;
  cameraState: number;
  simVersion: string;
  aircraftString: string;

  appGsxController: boolean;
  appAircraftBinary: boolean;
  appAircraftInterface: boolean;
  appProsimSdkConnected: boolean;
  appAutomationController: boolean;
  appAudioController: boolean;

  appOnGround: boolean;
  appEnginesRunning: boolean;
  appInMotion: boolean;
  appProfile: string;
  appAircraft: string;

  flightNumber: string;
  utcTime: string;
  utcDate: string;

  gsx: GsxLiveDto;
  messageLog: string[];
}

// ──────────────────────────────────────────────────────────────────────────
// GsxSettingsDto + ServiceConfigDto (read+write — GSX Settings tab)
// ──────────────────────────────────────────────────────────────────────────

export interface ServiceConfigDto {
  serviceType: GsxServiceType;
  serviceActivation: GsxServiceActivation;
  serviceConstraint: GsxServiceConstraint;
  // TimeSpan rides as total seconds via TimeSpanSecondsConverter
  minimumFlightDuration: number;
}

export interface GsxSettingsDto {
  profileName: string;

  doorStairHandling: boolean;
  doorCargoHandling: boolean;
  doorCateringHandling: boolean;
  doorOpenBoardActive: boolean;
  doorsCargoKeepOpenOnLoaded: boolean;
  doorsCargoKeepOpenOnUnloaded: boolean;
  closeDoorsOnFinal: boolean;

  callJetwayStairsOnPrep: boolean;
  callJetwayStairsDuringDeparture: boolean;
  callJetwayStairsOnArrival: boolean;
  removeStairsAfterDepature: number;
  removeJetwayStairsOnFinal: boolean;

  placeProsimStairsWalkaround: boolean;
  clearGroundEquipOnBeacon: boolean;
  gradualGroundEquipRemoval: boolean;
  connectGpuWithApuRunning: boolean;
  connectPca: number;
  pcaOverride: boolean;
  chockDelayMin: number;
  chockDelayMax: number;

  callReposition: boolean;
  callDeboardOnArrival: boolean;
  runDepartureDuringDeboarding: boolean;
  chimeOnParked: boolean;
  chimeOnDeboardComplete: boolean;
  departureServices: ServiceConfigDto[];

  refuelMethod: RefuelMethod;
  refuelRateKgSec: number;
  refuelTimeTargetSeconds: number;
  skipFuelOnTankering: boolean;
  refuelFinishOnHose: boolean;

  attachTugDuringBoarding: number;
  callPushbackWhenTugAttached: number;
  callPushbackOnBeacon: boolean;

  sequenceOnBeacon: boolean;
  seqDoorsCloseDelayMin: number;
  seqDoorsCloseDelayMax: number;
  seqJetwayRetractDelayMin: number;
  seqJetwayRetractDelayMax: number;
  seqGpuDisconnectDelayMin: number;
  seqGpuDisconnectDelayMax: number;

  operatorAutoSelect: boolean;
  operatorPreferences: string[];
  companyHubs: string[];

  skipWalkAround: boolean;
  skipCrewQuestion: boolean;
  skipFollowMe: boolean;
  keepDirectionMenuOpen: boolean;
  answerCabinCallGround: boolean;
  delayCabinCallGround: number;
  answerCabinCallAir: boolean;
  delayCabinCallAir: number;

  finalDelayMin: number;
  finalDelayMax: number;
  fuelSaveLoadFob: boolean;
  randomizePax: boolean;
  chancePerSeat: number;

  autoDeiceEnabled: boolean;
  autoDeiceFluid: AutoDeiceFluid;
}

// ──────────────────────────────────────────────────────────────────────────
// AudioDto + AudioMappingDto (read+write — Audio Settings tab)
// ──────────────────────────────────────────────────────────────────────────

export interface AudioMappingDto {
  channel: AudioChannel;
  device: string;
  binary: string;
  useLatch: boolean;
  onlyActive: boolean;
}

export interface AudioDto {
  isCoreAudioSelected: boolean;
  audioAcpSide: AcpSide;
  audioDeviceFlow: DataFlow;
  audioDeviceState: DeviceState;
  mappings: AudioMappingDto[];
  blacklist: string[];
  startupVolumes: Partial<Record<AudioChannel, number>>;
  startupUnmute: Partial<Record<AudioChannel, boolean>>;
}

// ──────────────────────────────────────────────────────────────────────────
// AppSettingsDto (read+write — App Settings tab)
// ──────────────────────────────────────────────────────────────────────────

export interface AppSettingsDto {
  displayUnitDefault: DisplayUnit;
  displayUnitSource: DisplayUnitSource;
  displayUnitCurrent: DisplayUnit;

  prosimWeightBag: number;
  fuelResetDefaultKg: number;
  fuelCompareVariance: number;
  fuelRoundUp100: boolean;

  dingOnStartup: boolean;
  dingOnFinal: boolean;

  cargoPercentChangePerSec: number;
  doorCargoDelay: number;
  doorCargoOpenDelay: number;

  resetGsxStateVarsFlight: boolean;
  restartGsxOnTaxiIn: boolean;
  restartGsxStartupFail: boolean;
  gsxMenuStartupMaxFail: number;

  runGsxService: boolean;
  runAudioService: boolean;
  useSayIntentions: boolean;
  allowManualChecklistOverride: boolean;
  openAppWindowOnStart: boolean;

  proSimSdkPath: string;

  solariAnimationEnabled: boolean;
  currentTheme: string;

  webServerEnabled: boolean;
  webServerPort: number;
  webServerBindAll: boolean;
  webServerAuthToken: string;
}

// ──────────────────────────────────────────────────────────────────────────
// Option lists — kept here so panels share the same labels and order
// ──────────────────────────────────────────────────────────────────────────

export const AUDIO_CHANNELS: AudioChannel[] = [
  "VHF1", "VHF2", "VHF3", "HF1", "HF2", "INT", "CAB", "PA",
];

export const ACP_SIDE_OPTIONS: { value: AcpSide; label: string }[] = [
  { value: "CPT", label: "Captain" },
  { value: "FO", label: "First Officer" },
];

export const DATA_FLOW_OPTIONS: { value: DataFlow; label: string }[] = [
  { value: "Render", label: "Render" },
  { value: "Capture", label: "Capture" },
  { value: "All", label: "All" },
];

export const DEVICE_STATE_OPTIONS: { value: DeviceState; label: string }[] = [
  { value: "Active", label: "Active" },
  { value: "Disabled", label: "Disabled" },
  { value: "NotPresent", label: "NotPresent" },
  { value: "Unplugged", label: "Unplugged" },
  { value: "MaskAll", label: "MaskAll" },
];

export const DISPLAY_UNIT_OPTIONS: { value: DisplayUnit; label: string }[] = [
  { value: "KG", label: "kg" },
  { value: "LB", label: "lb" },
];

export const DISPLAY_UNIT_SOURCE_OPTIONS: { value: DisplayUnitSource; label: string }[] = [
  { value: "App", label: "App" },
  { value: "Aircraft", label: "Aircraft" },
];

export const REFUEL_METHOD_OPTIONS: { value: RefuelMethod; label: string }[] = [
  { value: "FixedRate", label: "Fixed Rate" },
  { value: "DynamicRate", label: "Dynamic Rate" },
];

export const AUTO_DEICE_FLUID_OPTIONS: { value: AutoDeiceFluid; label: string }[] = [
  { value: "TypeI100", label: "Type I @ 100%" },
  { value: "TypeI75", label: "Type I @ 75%" },
  { value: "TypeII100", label: "Type II @ 100%" },
  { value: "TypeII75", label: "Type II @ 75%" },
  { value: "TypeIV100", label: "Type IV @ 100%" },
  { value: "TypeIV75", label: "Type IV @ 75%" },
];

export const TUG_OPTIONS: { value: number; label: string }[] = [
  { value: 0, label: "Don't Answer" },
  { value: 1, label: "No" },
  { value: 2, label: "Yes" },
];

export const PUSHBACK_TIMING_OPTIONS: { value: number; label: string }[] = [
  { value: 0, label: "False" },
  { value: 1, label: "After Departure Services" },
  { value: 2, label: "After Final LS" },
];

export const CONNECT_PCA_OPTIONS: { value: number; label: string }[] = [
  { value: 0, label: "False" },
  { value: 1, label: "True" },
  { value: 2, label: "Only on jetway stand" },
];

export const REMOVE_STAIRS_OPTIONS: { value: number; label: string }[] = [
  { value: 0, label: "False" },
  { value: 1, label: "True" },
  { value: 2, label: "Only on jetway stand" },
];

export const SERVICE_TYPE_OPTIONS: { value: GsxServiceType; label: string }[] = [
  { value: "Unknown", label: "Unknown" },
  { value: "Reposition", label: "Reposition" },
  { value: "Refuel", label: "Refuel" },
  { value: "Catering", label: "Catering" },
  { value: "Boarding", label: "Boarding" },
  { value: "Pushback", label: "Pushback" },
  { value: "Deice", label: "Deice" },
  { value: "Deboarding", label: "Deboarding" },
  { value: "GPU", label: "GPU" },
  { value: "Water", label: "Water" },
  { value: "Lavatory", label: "Lavatory" },
  { value: "Jetway", label: "Jetway" },
  { value: "Stairs", label: "Stairs" },
  { value: "Cleaning", label: "Cleaning" },
];

export const SERVICE_ACTIVATION_OPTIONS: { value: GsxServiceActivation; label: string }[] = [
  { value: "Skip", label: "Skip / Ignore" },
  { value: "Manual", label: "Manual by User" },
  { value: "AfterCalled", label: "Previous Service called" },
  { value: "AfterRequested", label: "Previous Service requested" },
  { value: "AfterActive", label: "Previous Service active" },
  { value: "AfterPrevCompleted", label: "Previous Service completed" },
  { value: "AfterAllCompleted", label: "All Services completed" },
];

export const SERVICE_CONSTRAINT_OPTIONS: { value: GsxServiceConstraint; label: string }[] = [
  { value: "NoneAlways", label: "None" },
  { value: "FirstLeg", label: "Only Departure" },
  { value: "TurnAround", label: "Only Turn" },
  { value: "CompanyHub", label: "Only on Hub" },
  { value: "NonCompanyHub", label: "Only on Non-Hub" },
];

// ──────────────────────────────────────────────────────────────────────────
// OFP DTO + command wire shapes
// ──────────────────────────────────────────────────────────────────────────

export interface WeatherDto {
  airport: string;
  atis: string;
  metar: string;
  taf: string;
  activeRunway: string;
  windDirection: number | null;
  windSpeed: number | null;
}

export interface OfpDto {
  isOfpLoaded: boolean;
  departureIcao: string;
  arrivalIcao: string;
  alternateIcao: string;
  flightNumber: string;
  departurePlanRwy: string;
  arrivalPlanRwy: string;
  cruiseAltitude: string;
  blockFuelKg: string;
  blockTimeFormatted: string;
  paxCount: string;
  airDistance: string;

  pendingArrivalGate: string;
  gateAssignmentStatus: string;
  gsxAssignmentStatus: string;
  assignedArrivalGate: string;

  departureWeather: WeatherDto | null;
  arrivalWeather: WeatherDto | null;
  weatherStatus: string;
  isRefreshingWeather: boolean;
  cpdlcStation: string;
  weatherFetchedAt: string | null;

  pushbackPreference: PushbackPreference;
  useSayIntentions: boolean;
  sayIntentionsActive: boolean;
}

export interface ConfirmArrivalGateRequest {
  gate: string;
}

export interface SetPushbackPreferenceRequest {
  preference: PushbackPreference;
}

export interface GateAssignmentDto {
  pendingArrivalGate: string;
  gateAssignmentStatus: string;
  gsxAssignmentStatus: string;
}

export interface PushbackPreferenceResponseDto {
  preference: PushbackPreference;
}

export interface WeatherSnapshotDto {
  departureWeather: WeatherDto | null;
  arrivalWeather: WeatherDto | null;
  weatherStatus: string;
  isRefreshingWeather: boolean;
  cpdlcStation: string;
  weatherFetchedAt: string | null;
}

export const PUSHBACK_OPTIONS: { value: PushbackPreference; label: string }[] = [
  { value: "Straight", label: "Straight" },
  { value: "TailLeft", label: "Tail Left" },
  { value: "TailRight", label: "Tail Right" },
];

// ──────────────────────────────────────────────────────────────────────────
// Aircraft Profiles (CRUD) wire shapes
// ──────────────────────────────────────────────────────────────────────────

export interface ProfileSummaryDto {
  name: string;
  matchType: ProfileMatchType;
  matchString: string;
  isActive: boolean;
  isDefault: boolean;
}

export interface ProfilesListDto {
  activeName: string;
  profiles: ProfileSummaryDto[];
  currentAirline: string;
  currentTitle: string;
  currentProfile: string;
}

export interface SetActiveProfileRequest { name: string; }
export interface CloneProfileRequest { sourceName: string; newName?: string; }
export interface RenameProfileRequest { oldName: string; newName: string; }
export interface UpdateProfileMetadataRequest {
  name: string;
  matchType: ProfileMatchType;
  matchString: string;
}
export interface DeleteProfileRequest { name: string; }

export const PROFILE_MATCH_TYPE_OPTIONS: { value: ProfileMatchType; label: string }[] = [
  { value: "Default", label: "Default (fallback)" },
  { value: "Airline", label: "Airline (starts with)" },
  { value: "Title", label: "Title / Livery (contains)" },
];

// ──────────────────────────────────────────────────────────────────────────
// Theme JSON (mirrors the WPF Themes/<name>.json shape)
// ──────────────────────────────────────────────────────────────────────────

export interface FlightPhaseColors {
  atGate: string;
  taxiOut: string;
  inFlight: string;
  approach: string;
  arrived: string;
}

export interface ThemeColors {
  primaryColor: string;
  secondaryColor: string;
  accentColor: string;
  headerBackground: string;
  tabBarBackground: string;
  contentBackground: string;
  sectionBackground: string;
  headerText: string;
  contentText: string;
  categoryText: string;
  flightPhaseColors: FlightPhaseColors;
}

export interface ThemeFile {
  name: string;
  description: string;
  colors: ThemeColors;
}

// ──────────────────────────────────────────────────────────────────────────
// Checklists
// ──────────────────────────────────────────────────────────────────────────

export interface ChecklistItemDto {
  label: string;
  value: string;
  dataRef: string;
  isNote: boolean;
  isSeparator: boolean;
  isManual: boolean;
  isChecked: boolean;
}

export interface ChecklistSectionDto {
  title: string;
  items: ChecklistItemDto[];
}

export interface ChecklistDto {
  currentChecklistName: string;
  availableChecklists: string[];
  aircraftType: string;
  name: string;
  currentSectionIndex: number;
  currentItemIndex: number;
  isSectionComplete: boolean;
  allowManualOverride: boolean;
  sections: ChecklistSectionDto[];
}

export interface SelectChecklistRequest {
  name: string;
}

export interface SelectSectionRequest {
  sectionIndex: number;
}

export interface ToggleItemRequest {
  sectionIndex: number;
  itemIndex: number;
}

export interface ResetSectionRequest {
  sectionIndex: number;
}
