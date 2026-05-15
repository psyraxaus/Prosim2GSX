import { jsx as _jsx } from "react/jsx-runtime";
import styles from "./TabBar.module.css";
// Order matches the WPF AppWindow tabs, with the new perf tabs appended
// after the canonical web-only data tabs (W&B / Fuel) and before
// Checklists. The WPF surface dropped these so the web is canonical.
const TABS = [
    { key: "flightStatus", label: "Flight Status" },
    { key: "init", label: "INIT" },
    { key: "ofp", label: "OFP" },
    { key: "loadsheet", label: "Loadsheet" },
    { key: "weightBalance", label: "W&B" },
    { key: "fuel", label: "Fuel" },
    { key: "takeoff", label: "Takeoff" },
    { key: "landing", label: "Landing" },
    { key: "checklists", label: "Checklists" },
    { key: "gsxSettings", label: "GSX Settings" },
    { key: "aircraftProfiles", label: "Aircraft Profiles" },
    { key: "audioSettings", label: "Audio Settings" },
    { key: "appSettings", label: "App Settings" },
];
export function TabBar({ active, onSelect }) {
    return (_jsx("nav", { className: styles.tabBar, role: "tablist", children: TABS.map((tab) => (_jsx("button", { role: "tab", "aria-selected": tab.key === active, className: `${styles.tab} ${tab.key === active ? styles.active : ""}`, onClick: () => onSelect(tab.key), children: tab.label }, tab.key))) }));
}
