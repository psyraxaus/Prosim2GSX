import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useCallback, useEffect, useRef, useState } from "react";
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
    const stripRef = useRef(null);
    // overflowing: the strip is wider than its slot (chevrons are relevant).
    // canLeft/canRight: there is hidden content in that direction.
    const [overflowing, setOverflowing] = useState(false);
    const [canLeft, setCanLeft] = useState(false);
    const [canRight, setCanRight] = useState(false);
    const updateArrows = useCallback(() => {
        const el = stripRef.current;
        if (!el)
            return;
        const max = el.scrollWidth - el.clientWidth;
        setOverflowing(max > 1);
        setCanLeft(el.scrollLeft > 1);
        setCanRight(el.scrollLeft < max - 1);
    }, []);
    useEffect(() => {
        const el = stripRef.current;
        if (!el)
            return;
        updateArrows();
        const ro = new ResizeObserver(updateArrows);
        ro.observe(el);
        el.addEventListener("scroll", updateArrows, { passive: true });
        return () => {
            ro.disconnect();
            el.removeEventListener("scroll", updateArrows);
        };
    }, [updateArrows]);
    // Keep the selected tab visible — selecting a clipped tab should never
    // leave it off-screen.
    useEffect(() => {
        const el = stripRef.current;
        if (!el)
            return;
        el.querySelector(`[data-tabkey="${active}"]`)
            ?.scrollIntoView({ behavior: "smooth", inline: "nearest", block: "nearest" });
    }, [active]);
    const scrollByDir = (dir) => {
        const el = stripRef.current;
        if (el)
            el.scrollBy({ left: dir * el.clientWidth * 0.8, behavior: "smooth" });
    };
    return (_jsxs("div", { className: styles.tabBarWrap, children: [overflowing && (_jsx("button", { type: "button", className: `${styles.chevron} ${styles.chevronLeft}`, "aria-label": "Scroll tabs left", disabled: !canLeft, onClick: () => scrollByDir(-1), children: "\u2039" })), _jsx("nav", { className: styles.tabBar, role: "tablist", ref: stripRef, children: TABS.map((tab) => (_jsx("button", { "data-tabkey": tab.key, role: "tab", "aria-selected": tab.key === active, className: `${styles.tab} ${tab.key === active ? styles.active : ""}`, onClick: () => onSelect(tab.key), children: tab.label }, tab.key))) }), overflowing && (_jsx("button", { type: "button", className: `${styles.chevron} ${styles.chevronRight}`, "aria-label": "Scroll tabs right", disabled: !canRight, onClick: () => scrollByDir(1), children: "\u203A" }))] }));
}
