import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useAppState } from "../state/AppStateContext";
import { ConnectionIndicator } from "./ConnectionIndicator";
import { SplitFlap } from "./SplitFlap";
import styles from "./Header.module.css";
export function Header() {
    const { state } = useAppState();
    const fs = state.flightStatus;
    const flightNumber = fs?.flightNumber ?? "--------";
    const utcTime = fs?.utcTime ?? "--:--Z";
    const utcDate = fs?.utcDate ?? "------";
    return (_jsxs("header", { className: styles.header, children: [_jsxs("div", { className: styles.brand, children: [_jsx("span", { className: styles.brandName, children: "PROSIM2GSX" }), _jsx("span", { className: styles.brandTag, children: "WEB" })] }), _jsxs("div", { className: styles.flapCenter, children: [_jsx(SplitFlap, { text: flightNumber, count: 8, staggerDelayMs: 80 }), _jsx("span", { className: styles.flapLabel, children: "FLT NO" })] }), _jsxs("div", { className: styles.flapRight, children: [_jsxs("div", { className: styles.flapBlock, children: [_jsx(SplitFlap, { text: utcTime, count: 6, staggerDelayMs: 80 }), _jsx("span", { className: styles.flapLabel, children: "UTC" })] }), _jsxs("div", { className: styles.flapBlock, children: [_jsx(SplitFlap, { text: utcDate, count: 6, staggerDelayMs: 80 }), _jsx("span", { className: styles.flapLabel, children: "DATE" })] }), _jsx(ConnectionIndicator, { status: state.connection })] })] }));
}
