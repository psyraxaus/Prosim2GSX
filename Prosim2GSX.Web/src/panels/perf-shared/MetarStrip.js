import { jsxs as _jsxs, jsx as _jsx } from "react/jsx-runtime";
import styles from "./PerfShared.module.css";
export function MetarStrip({ icao, metarText, fetchedAt }) {
    const trimmed = (metarText ?? "").trim();
    const stamp = fetchedAt ? formatStamp(fetchedAt) : "";
    return (_jsxs("div", { className: styles.metarStrip, children: [_jsxs("div", { className: styles.metarHeader, children: [_jsxs("span", { children: ["METAR ", icao || "----"] }), stamp && _jsxs("span", { children: ["\u00B7 ", stamp] })] }), trimmed ? (_jsx("div", { className: styles.metarText, children: trimmed })) : (_jsx("div", { className: styles.metarMissing, children: "No METAR available" }))] }));
}
function formatStamp(iso) {
    try {
        const d = new Date(iso);
        const h = String(d.getUTCHours()).padStart(2, "0");
        const m = String(d.getUTCMinutes()).padStart(2, "0");
        return `${h}:${m}Z`;
    }
    catch {
        return "";
    }
}
