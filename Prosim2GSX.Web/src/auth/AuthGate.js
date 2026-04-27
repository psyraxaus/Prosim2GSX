import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useState } from "react";
import { storeToken } from "./auth";
import styles from "./AuthGate.module.css";
// First-paint screen when no token is in localStorage. The user pastes
// the GUID shown in the WPF App Settings tab (or the URL is loaded with
// a #token=... hash, in which case the bootstrap in main.tsx fills in
// localStorage before React mounts and this gate is skipped).
export function AuthGate({ onAuth }) {
    const [value, setValue] = useState("");
    function submit(e) {
        e.preventDefault();
        const trimmed = value.trim();
        if (!trimmed)
            return;
        storeToken(trimmed);
        onAuth();
    }
    return (_jsx("div", { className: styles.gate, children: _jsxs("form", { className: styles.card, onSubmit: submit, children: [_jsx("h1", { className: styles.title, children: "Prosim2GSX" }), _jsxs("p", { className: styles.subtitle, children: ["Paste the auth token from the desktop app's", _jsx("br", {}), _jsx("strong", { children: "App Settings \u2192 Web Interface" }), " panel"] }), _jsx("input", { autoFocus: true, className: styles.input, type: "password", value: value, onChange: (e) => setValue(e.target.value), placeholder: "Auth token", spellCheck: false, autoCapitalize: "off", autoComplete: "off" }), _jsx("button", { type: "submit", className: styles.button, disabled: !value.trim(), children: "Connect" }), _jsx("p", { className: styles.hint, children: "Or scan the QR code on the desktop app to skip this step." })] }) }));
}
