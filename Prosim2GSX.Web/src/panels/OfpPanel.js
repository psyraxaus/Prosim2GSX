import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useEffect, useState } from "react";
import { useApi } from "../api/useApi";
import { useAppState } from "../state/AppStateContext";
import { Section } from "../components/forms/Section";
import { PrimaryButton } from "../components/forms/PrimaryButton";
import { KorryPushbackButton } from "../components/KorryPushbackButton";
import styles from "./OfpPanel.module.css";
// Full OFP tab. Mirrors the WPF ModelOfp surface: read-only OFP summary,
// arrival-gate input + Confirm + Send Now, pushback preference, weather
// cards. Auto-refreshes weather on tab activation per the locked decision
// in project memory phase8_design_decisions.
//
// Live updates flow in via the WS "ofp" channel (subscribed by
// StateWebSocketHandler — broadcasts OfpState property changes and
// GsxController.PushbackPreferenceChanged events). The panel REST-fetches
// once on mount; subsequent updates merge into AppStateContext.ofp.
export function OfpPanel() {
    const { get, post } = useApi();
    const { state, dispatch } = useAppState();
    const [arrivalGateInput, setArrivalGateInput] = useState("");
    const [busy, setBusy] = useState(null);
    const [error, setError] = useState(null);
    async function loadOfp() {
        setError(null);
        try {
            const dto = await get("/ofp");
            dispatch({ type: "set", channel: "ofp", state: dto });
        }
        catch (e) {
            setError(e.message ?? "Failed to load");
        }
    }
    async function refreshWeather() {
        setBusy("weather");
        setError(null);
        try {
            // Server updates OfpState; WS pushes the patch. Discard the response —
            // we read from state.ofp.
            await post("/ofp/refresh-weather");
        }
        catch (e) {
            setError(e.message ?? "Weather refresh failed");
        }
        finally {
            setBusy(null);
        }
    }
    useEffect(() => {
        // Both fire on mount; mount = tab activation because App.tsx
        // conditionally renders this component when active === "ofp".
        loadOfp();
        refreshWeather();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);
    const ofp = state.ofp;
    if (!ofp) {
        return (_jsx("div", { className: styles.loading, children: error ? `Error: ${error}` : "Loading OFP…" }));
    }
    async function confirmGate(e) {
        e.preventDefault();
        const gate = arrivalGateInput.trim().toUpperCase();
        if (!gate)
            return;
        setBusy("confirm");
        setError(null);
        try {
            const req = { gate };
            await post("/ofp/confirm-arrival-gate", req);
            setArrivalGateInput("");
        }
        catch (e) {
            setError(e.message ?? "Confirm failed");
        }
        finally {
            setBusy(null);
        }
    }
    async function sendNow() {
        setBusy("sendNow");
        setError(null);
        try {
            await post("/ofp/send-now");
        }
        catch (e) {
            setError(e.message ?? "Send Now failed");
        }
        finally {
            setBusy(null);
        }
    }
    async function setPushback(preference) {
        // Narrowing of `ofp` from the early-return check above is lost inside
        // this nested closure under TS strict mode, so re-guard here.
        if (!ofp || preference === ofp.pushbackPreference)
            return;
        setBusy("pushback");
        setError(null);
        try {
            const req = { preference };
            await post("/ofp/set-pushback-preference", req);
        }
        catch (e) {
            setError(e.message ?? "Pushback set failed");
        }
        finally {
            setBusy(null);
        }
    }
    return (_jsxs("div", { className: styles.panel, children: [error && _jsx("div", { className: styles.error, children: error }), _jsx(Section, { title: "Flight Information", children: ofp.isOfpLoaded ? (_jsxs("div", { className: styles.kvGrid, children: [_jsx(KV, { label: "Departure", value: ofp.departureIcao, mono: true }), _jsx(KV, { label: "Arrival", value: ofp.arrivalIcao, mono: true }), _jsx(KV, { label: "Alternate", value: ofp.alternateIcao || "—", mono: true }), _jsx(KV, { label: "Flight", value: ofp.flightNumber || "—", mono: true }), _jsx(KV, { label: "Plan RWY Out", value: ofp.departurePlanRwy || "—", mono: true }), _jsx(KV, { label: "Plan RWY In", value: ofp.arrivalPlanRwy || "—", mono: true }), _jsx(KV, { label: "Cruise Alt", value: ofp.cruiseAltitude || "—", mono: true }), _jsx(KV, { label: "Block Fuel", value: ofp.blockFuelKg || "—", mono: true }), _jsx(KV, { label: "Block Time", value: ofp.blockTimeFormatted || "—", mono: true }), _jsx(KV, { label: "Pax", value: ofp.paxCount || "—", mono: true }), _jsx(KV, { label: "Air Distance", value: ofp.airDistance || "—", mono: true })] })) : (_jsx("div", { className: styles.empty, children: "No OFP loaded. Import a flight plan in ProSim." })) }), _jsx(Section, { title: "Pushback Direction", children: _jsxs("div", { className: styles.korryGroup, role: "group", "aria-label": "Pushback direction", children: [_jsx(KorryPushbackButton, { arrow: "tailLeft", label: "TAIL LEFT", subtitle: "NOSE RIGHT", isActive: ofp.pushbackPreference === "TailLeft", onClick: () => setPushback("TailLeft"), disabled: busy === "pushback", title: "Auto-select 'Nose Right / Tail Left' when GSX shows the direction menu." }), _jsx(KorryPushbackButton, { arrow: "straight", label: "STRAIGHT", subtitle: "STRAIGHT BACK", isActive: ofp.pushbackPreference === "Straight", onClick: () => setPushback("Straight"), disabled: busy === "pushback", title: "Auto-select 'Straight pushback' when GSX shows the direction menu." }), _jsx(KorryPushbackButton, { arrow: "tailRight", label: "TAIL RIGHT", subtitle: "NOSE LEFT", isActive: ofp.pushbackPreference === "TailRight", onClick: () => setPushback("TailRight"), disabled: busy === "pushback", title: "Auto-select 'Nose Left / Tail Right' when GSX shows the direction menu." })] }) }), _jsxs(Section, { title: "Arrival Gate Assignment", children: [_jsxs("form", { className: styles.gateForm, onSubmit: confirmGate, children: [_jsx("input", { type: "text", value: arrivalGateInput, onChange: (e) => setArrivalGateInput(e.target.value.toUpperCase()), placeholder: "GATE", disabled: !ofp.isOfpLoaded || busy === "confirm", className: styles.gateInput, autoCapitalize: "characters", spellCheck: false }), _jsx(PrimaryButton, { onClick: () => { }, disabled: !ofp.isOfpLoaded || !arrivalGateInput.trim() || busy === "confirm", children: busy === "confirm" ? "Confirming…" : "Confirm" }), _jsx(PrimaryButton, { variant: "secondary", onClick: sendNow, disabled: !ofp.pendingArrivalGate || busy === "sendNow", children: busy === "sendNow" ? "Sending…" : "Send Now" })] }), ofp.pendingArrivalGate && (_jsxs("div", { className: styles.pending, children: [_jsx("span", { className: styles.pendingLabel, children: "Pending" }), _jsx("span", { className: styles.pendingValue, children: ofp.pendingArrivalGate })] })), ofp.assignedArrivalGate && (_jsxs("div", { className: styles.assigned, children: [_jsx("span", { className: styles.assignedLabel, children: "GSX Assigned" }), _jsx("span", { className: styles.assignedValue, children: ofp.assignedArrivalGate })] })), ofp.gateAssignmentStatus && (_jsx(StatusLine, { label: "ATC", value: ofp.gateAssignmentStatus })), ofp.gsxAssignmentStatus && (_jsx(StatusLine, { label: "GSX", value: ofp.gsxAssignmentStatus }))] }), _jsxs(Section, { title: "Weather", hint: ofp.sayIntentionsActive ? "" : "SayIntentions inactive", children: [_jsxs("div", { className: styles.weatherToolbar, children: [_jsx(PrimaryButton, { variant: "secondary", onClick: refreshWeather, disabled: busy === "weather" || !ofp.sayIntentionsActive, children: busy === "weather" || ofp.isRefreshingWeather ? "Refreshing…" : "Refresh Weather" }), ofp.weatherStatus && _jsx("span", { className: styles.weatherStatus, children: ofp.weatherStatus })] }), _jsxs("div", { className: styles.weatherGrid, children: [_jsx(WeatherCard, { title: `Departure ${ofp.departureIcao || ""}`, wx: ofp.departureWeather }), _jsx(WeatherCard, { title: `Arrival ${ofp.arrivalIcao || ""}`, wx: ofp.arrivalWeather })] })] })] }));
}
function KV({ label, value, mono }) {
    return (_jsxs("div", { className: styles.kv, children: [_jsx("span", { className: styles.kvLabel, children: label }), _jsx("span", { className: mono ? styles.kvValueMono : styles.kvValue, children: value })] }));
}
function StatusLine({ label, value }) {
    return (_jsxs("div", { className: styles.statusLine, children: [_jsx("span", { className: styles.statusLabel, children: label }), _jsx("span", { className: styles.statusValue, children: value })] }));
}
function WeatherCard({ title, wx }) {
    if (!wx) {
        return (_jsxs("div", { className: styles.weatherCard, children: [_jsx("div", { className: styles.weatherTitle, children: title }), _jsx("div", { className: styles.empty, children: "No weather data." })] }));
    }
    return (_jsxs("div", { className: styles.weatherCard, children: [_jsx("div", { className: styles.weatherTitle, children: title }), wx.activeRunway && (_jsxs("div", { className: styles.weatherRow, children: [_jsx("span", { className: styles.weatherLabel, children: "Active RWY" }), _jsx("span", { className: styles.weatherValue, children: wx.activeRunway })] })), (wx.windDirection !== null || wx.windSpeed !== null) && (_jsxs("div", { className: styles.weatherRow, children: [_jsx("span", { className: styles.weatherLabel, children: "Wind" }), _jsxs("span", { className: styles.weatherValue, children: [wx.windDirection !== null ? `${wx.windDirection}°` : "—", " / ", wx.windSpeed !== null ? `${wx.windSpeed}kt` : "—"] })] })), wx.metar && (_jsxs("div", { className: styles.weatherBlock, children: [_jsx("div", { className: styles.weatherLabel, children: "METAR" }), _jsx("div", { className: styles.weatherValueBlock, children: wx.metar })] })), wx.atis && (_jsxs("div", { className: styles.weatherBlock, children: [_jsx("div", { className: styles.weatherLabel, children: "ATIS" }), _jsx("div", { className: styles.weatherValueBlock, children: wx.atis })] })), wx.taf && (_jsxs("div", { className: styles.weatherBlock, children: [_jsx("div", { className: styles.weatherLabel, children: "TAF" }), _jsx("div", { className: styles.weatherValueBlock, children: wx.taf })] }))] }));
}
