import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import styles from "./ConnectionIndicator.module.css";
const LABEL = {
    connecting: "Connecting…",
    open: "Connected",
    reconnecting: "Reconnecting…",
    closed: "Disconnected",
};
// Coloured dot + label. Green when WS is open, amber while reconnecting,
// red when closed/disconnected, neutral while the initial connect attempt
// is in flight.
export function ConnectionIndicator({ status }) {
    return (_jsxs("div", { className: styles.indicator, title: LABEL[status], children: [_jsx("span", { className: `${styles.dot} ${styles[status]}`, "aria-hidden": "true" }), _jsx("span", { className: styles.label, children: LABEL[status] })] }));
}
