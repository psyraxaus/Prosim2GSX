// Wire-contract types. Mirror the C# DTOs in Prosim2GSX/Web/Contracts/
// (and the enum value names from the CFIT/Prosim source). Wire format
// rules are locked in project memory project_web_json_contract_decisions:
//   - camelCase property names
//   - enum values as string literals (declared C# names)
//   - TimeSpan as total seconds (number)
//   - dictionaries with enum keys → object with string-name keys
//   - dictionaries with int order keys → arrays
// ──────────────────────────────────────────────────────────────────────────
// Option lists — kept here so panels share the same labels and order
// ──────────────────────────────────────────────────────────────────────────
export const AUDIO_CHANNELS = [
    "VHF1", "VHF2", "VHF3", "HF1", "HF2", "INT", "CAB", "PA",
];
export const ACP_SIDE_OPTIONS = [
    { value: "CPT", label: "Captain" },
    { value: "FO", label: "First Officer" },
];
export const DATA_FLOW_OPTIONS = [
    { value: "Render", label: "Render" },
    { value: "Capture", label: "Capture" },
    { value: "All", label: "All" },
];
export const DEVICE_STATE_OPTIONS = [
    { value: "Active", label: "Active" },
    { value: "Disabled", label: "Disabled" },
    { value: "NotPresent", label: "NotPresent" },
    { value: "Unplugged", label: "Unplugged" },
    { value: "MaskAll", label: "MaskAll" },
];
export const DISPLAY_UNIT_OPTIONS = [
    { value: "KG", label: "kg" },
    { value: "LB", label: "lb" },
];
export const DISPLAY_UNIT_SOURCE_OPTIONS = [
    { value: "App", label: "App" },
    { value: "Aircraft", label: "Aircraft" },
];
export const REFUEL_METHOD_OPTIONS = [
    { value: "FixedRate", label: "Fixed Rate" },
    { value: "DynamicRate", label: "Dynamic Rate" },
];
export const AUTO_DEICE_FLUID_OPTIONS = [
    { value: "TypeI100", label: "Type I @ 100%" },
    { value: "TypeI75", label: "Type I @ 75%" },
    { value: "TypeII100", label: "Type II @ 100%" },
    { value: "TypeII75", label: "Type II @ 75%" },
    { value: "TypeIV100", label: "Type IV @ 100%" },
    { value: "TypeIV75", label: "Type IV @ 75%" },
];
export const TUG_OPTIONS = [
    { value: 0, label: "Don't Answer" },
    { value: 1, label: "No" },
    { value: 2, label: "Yes" },
];
export const PUSHBACK_TIMING_OPTIONS = [
    { value: 0, label: "False" },
    { value: 1, label: "After Departure Services" },
    { value: 2, label: "After Final LS" },
];
export const CONNECT_PCA_OPTIONS = [
    { value: 0, label: "False" },
    { value: 1, label: "True" },
    { value: 2, label: "Only on jetway stand" },
];
export const REMOVE_STAIRS_OPTIONS = [
    { value: 0, label: "False" },
    { value: 1, label: "True" },
    { value: 2, label: "Only on jetway stand" },
];
export const SERVICE_TYPE_OPTIONS = [
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
export const SERVICE_ACTIVATION_OPTIONS = [
    { value: "Skip", label: "Skip / Ignore" },
    { value: "Manual", label: "Manual by User" },
    { value: "AfterCalled", label: "Previous Service called" },
    { value: "AfterRequested", label: "Previous Service requested" },
    { value: "AfterActive", label: "Previous Service active" },
    { value: "AfterPrevCompleted", label: "Previous Service completed" },
    { value: "AfterAllCompleted", label: "All Services completed" },
];
export const SERVICE_CONSTRAINT_OPTIONS = [
    { value: "NoneAlways", label: "None" },
    { value: "FirstLeg", label: "Only Departure" },
    { value: "TurnAround", label: "Only Turn" },
    { value: "CompanyHub", label: "Only on Hub" },
    { value: "NonCompanyHub", label: "Only on Non-Hub" },
];
