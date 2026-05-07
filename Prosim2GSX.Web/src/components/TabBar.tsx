import styles from "./TabBar.module.css";

export type TabKey =
  | "flightStatus"
  | "init"
  | "ofp"
  | "loadsheet"
  | "weightBalance"
  | "checklists"
  | "gsxSettings"
  | "aircraftProfiles"
  | "audioSettings"
  | "appSettings";

interface Tab {
  key: TabKey;
  label: string;
}

// Order matches the WPF AppWindow tabs:
// Flight Status → INIT → OFP → Loadsheet → W&B → Checklists → GSX Settings → Aircraft Profiles → Audio Settings → App Settings.
const TABS: Tab[] = [
  { key: "flightStatus", label: "Flight Status" },
  { key: "init", label: "INIT" },
  { key: "ofp", label: "OFP" },
  { key: "loadsheet", label: "Loadsheet" },
  { key: "weightBalance", label: "W&B" },
  { key: "checklists", label: "Checklists" },
  { key: "gsxSettings", label: "GSX Settings" },
  { key: "aircraftProfiles", label: "Aircraft Profiles" },
  { key: "audioSettings", label: "Audio Settings" },
  { key: "appSettings", label: "App Settings" },
];

interface Props {
  active: TabKey;
  onSelect: (key: TabKey) => void;
}

export function TabBar({ active, onSelect }: Props) {
  return (
    <nav className={styles.tabBar} role="tablist">
      {TABS.map((tab) => (
        <button
          key={tab.key}
          role="tab"
          aria-selected={tab.key === active}
          className={`${styles.tab} ${tab.key === active ? styles.active : ""}`}
          onClick={() => onSelect(tab.key)}
        >
          {tab.label}
        </button>
      ))}
    </nav>
  );
}
