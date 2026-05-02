import { jsx as _jsx } from "react/jsx-runtime";
import styles from "./TabBar.module.css";
// Order matches the WPF AppWindow tabs:
// Flight Status → OFP → Checklists → GSX Settings → Aircraft Profiles → Audio Settings → App Settings.
const TABS = [
    { key: "flightStatus", label: "Flight Status" },
    { key: "ofp", label: "OFP" },
    { key: "checklists", label: "Checklists" },
    { key: "gsxSettings", label: "GSX Settings" },
    { key: "aircraftProfiles", label: "Aircraft Profiles" },
    { key: "audioSettings", label: "Audio Settings" },
    { key: "appSettings", label: "App Settings" },
];
export function TabBar({ active, onSelect }) {
    return (_jsx("nav", { className: styles.tabBar, role: "tablist", children: TABS.map((tab) => (_jsx("button", { role: "tab", "aria-selected": tab.key === active, className: `${styles.tab} ${tab.key === active ? styles.active : ""}`, onClick: () => onSelect(tab.key), children: tab.label }, tab.key))) }));
}
