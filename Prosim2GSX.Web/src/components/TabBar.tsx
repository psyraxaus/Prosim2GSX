import styles from "./TabBar.module.css";

export type TabKey = "flightStatus" | "audioSettings" | "appSettings" | "gsxSettings";

interface Tab {
  key: TabKey;
  label: string;
}

const TABS: Tab[] = [
  { key: "flightStatus", label: "Flight Status" },
  { key: "audioSettings", label: "Audio Settings" },
  { key: "appSettings", label: "App Settings" },
  { key: "gsxSettings", label: "GSX Settings" },
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
