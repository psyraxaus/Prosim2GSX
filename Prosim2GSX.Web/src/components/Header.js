import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useAppState } from "../state/AppStateContext";
import { ConnectionIndicator } from "./ConnectionIndicator";
import styles from "./Header.module.css";
export function Header() {
    const { state } = useAppState();
    return (_jsxs("header", { className: styles.header, children: [_jsxs("div", { className: styles.brand, children: [_jsx("span", { className: styles.brandName, children: "PROSIM2GSX" }), _jsx("span", { className: styles.brandTag, children: "WEB" })] }), _jsx(ConnectionIndicator, { status: state.connection })] }));
}
