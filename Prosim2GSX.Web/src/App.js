import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useEffect, useState } from "react";
import { AUTH_FAIL_EVENT, getStoredToken } from "./auth/auth";
import { AuthGate } from "./auth/AuthGate";
import { AppStateProvider, useAppState } from "./state/AppStateContext";
import { useWebSocket } from "./ws/useWebSocket";
import { useApi } from "./api/useApi";
import { useTheme } from "./theme/useTheme";
import { Header } from "./components/Header";
import { TabBar } from "./components/TabBar";
import { FlightStatusPanel } from "./panels/FlightStatusPanel";
import { AudioSettingsPanel } from "./panels/AudioSettingsPanel";
import { AppSettingsPanel } from "./panels/AppSettingsPanel";
import { GsxSettingsPanel } from "./panels/GsxSettingsPanel";
import { OfpPanel } from "./panels/OfpPanel";
import { AircraftProfilesPanel } from "./panels/AircraftProfilesPanel";
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
    const { state, dispatch } = useAppState();
    useWebSocket(dispatch);
    const { get } = useApi();
    // Pre-fetch AppSettings once on app load so the active theme name is in
    // state.appSettings.currentTheme before any panel mounts. Without this
    // pre-fetch the theme would only apply once the user opened the App
    // Settings tab. The AppSettingsPanel still does its own fetch on mount
    // to populate its draft/baseline pair — it just sees the cached state.
    useEffect(() => {
        let cancelled = false;
        (async () => {
            try {
                const dto = await get("/appsettings");
                if (!cancelled)
                    dispatch({ type: "set", channel: "appSettings", state: dto });
            }
            catch {
                /* ignore — theme stays at the CSS default until something else loads */
            }
        })();
        return () => { cancelled = true; };
    }, [get, dispatch]);
    // Apply the active theme. Re-runs whenever currentTheme changes — both
    // the local "user saved a new theme" path and the cross-client "another
    // client / WPF window changed the theme" path (which arrives as a WS
    // patch on the appSettings channel into state.appSettings.currentTheme).
    const themeName = state.appSettings?.currentTheme ?? null;
    useTheme(themeName);
    const [tab, setTab] = useState("flightStatus");
    return (_jsxs("div", { className: styles.app, children: [_jsx(Header, {}), _jsx(TabBar, { active: tab, onSelect: setTab }), _jsxs("main", { className: styles.main, children: [tab === "flightStatus" && _jsx(FlightStatusPanel, {}), tab === "ofp" && _jsx(OfpPanel, {}), tab === "gsxSettings" && _jsx(GsxSettingsPanel, {}), tab === "aircraftProfiles" && _jsx(AircraftProfilesPanel, {}), tab === "audioSettings" && _jsx(AudioSettingsPanel, {}), tab === "appSettings" && _jsx(AppSettingsPanel, {})] })] }));
}
