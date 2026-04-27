import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useEffect, useState } from "react";
import { AUTH_FAIL_EVENT, getStoredToken } from "./auth/auth";
import { AuthGate } from "./auth/AuthGate";
import { AppStateProvider, useAppState } from "./state/AppStateContext";
import { useWebSocket } from "./ws/useWebSocket";
import { Header } from "./components/Header";
import { TabBar } from "./components/TabBar";
import { FlightStatusPanel } from "./panels/FlightStatusPanel";
import { AudioSettingsPanel } from "./panels/AudioSettingsPanel";
import { AppSettingsPanel } from "./panels/AppSettingsPanel";
import { GsxSettingsPanel } from "./panels/GsxSettingsPanel";
import { PanelPlaceholder } from "./components/PanelPlaceholder";
import styles from "./App.module.css";
export function App() {
    // The auth gate is shown until a token is in localStorage. Re-checked
    // when the AUTH_FAIL_EVENT fires (any 401 from REST or 1008 close from
    // WS routes through there) so the user is bumped back here cleanly.
    const [authed, setAuthed] = useState(() => !!getStoredToken());
    useEffect(() => {
        function onFail() {
            setAuthed(false);
        }
        window.addEventListener(AUTH_FAIL_EVENT, onFail);
        return () => window.removeEventListener(AUTH_FAIL_EVENT, onFail);
    }, []);
    if (!authed) {
        return _jsx(AuthGate, { onAuth: () => setAuthed(true) });
    }
    return (_jsx(AppStateProvider, { children: _jsx(AppShell, {}) }));
}
function AppShell() {
    const { dispatch } = useAppState();
    useWebSocket(dispatch);
    const [tab, setTab] = useState("flightStatus");
    return (_jsxs("div", { className: styles.app, children: [_jsx(Header, {}), _jsx(TabBar, { active: tab, onSelect: setTab }), _jsxs("main", { className: styles.main, children: [tab === "flightStatus" && _jsx(FlightStatusPanel, {}), tab === "ofp" && _jsx(PanelPlaceholder, { title: "OFP" }), tab === "gsxSettings" && _jsx(GsxSettingsPanel, {}), tab === "aircraftProfiles" && _jsx(PanelPlaceholder, { title: "Aircraft Profiles" }), tab === "audioSettings" && _jsx(AudioSettingsPanel, {}), tab === "appSettings" && _jsx(AppSettingsPanel, {})] })] }));
}
