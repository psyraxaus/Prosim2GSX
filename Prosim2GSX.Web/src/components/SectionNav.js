import { jsx as _jsx } from "react/jsx-runtime";
import styles from "./SectionNav.module.css";
// Vertical left-rail navigation on desktop, horizontal pill bar at top on
// narrow viewports. Mirrors the WPF GSX Settings tab's left sidebar
// (ViewAutomation.xaml) without requiring fixed pixel widths in CSS — the
// rail width adapts to the longest label, the pill bar scrolls horizontally
// when items overflow.
export function SectionNav({ items, active, onSelect }) {
    return (_jsx("nav", { className: styles.nav, role: "tablist", "aria-orientation": "vertical", children: items.map((item) => (_jsx("button", { role: "tab", "aria-selected": item.key === active, className: `${styles.item} ${item.key === active ? styles.active : ""}`, onClick: () => onSelect(item.key), children: item.label }, item.key))) }));
}
