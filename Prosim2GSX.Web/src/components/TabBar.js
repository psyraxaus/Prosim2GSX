import { jsx as _jsx } from "react/jsx-runtime";
import styles from "./TabBar.module.css";
const TABS = [
    { key: "flightStatus", label: "Flight Status" },
    { key: "audioSettings", label: "Audio Settings" },
    { key: "appSettings", label: "App Settings" },
    { key: "gsxSettings", label: "GSX Settings" },
];
export function TabBar({ active, onSelect }) {
    return (_jsx("nav", { className: styles.tabBar, role: "tablist", children: TABS.map((tab) => (_jsx("button", { role: "tab", "aria-selected": tab.key === active, className: `${styles.tab} ${tab.key === active ? styles.active : ""}`, onClick: () => onSelect(tab.key), children: tab.label }, tab.key))) }));
}
