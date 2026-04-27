import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import styles from "./PanelPlaceholder.module.css";
// Stand-in for each tab while Phase 7B builds the real panels. Mounted
// as a child of the active tab so the AppShell layout (header + tab bar
// + scrollable main area) is exercised end-to-end before the panel
// content lands.
export function PanelPlaceholder({ title }) {
    return (_jsxs("section", { className: styles.panel, children: [_jsx("h2", { className: styles.heading, children: title }), _jsx("p", { className: styles.body, children: "Phase 7B will populate this panel. The auth gate, WebSocket connection, and state context are live \u2014 open the browser console to watch deltas stream in." })] }));
}
