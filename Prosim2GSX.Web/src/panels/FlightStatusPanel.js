import { jsx as _jsx, jsxs as _jsxs, Fragment as _Fragment } from "react/jsx-runtime";
import { useEffect, useRef, useState } from "react";
import { useApi } from "../api/useApi";
import { useAppState } from "../state/AppStateContext";
import styles from "./FlightStatusPanel.module.css";
// Read-only Monitor surface. Initial REST load on mount; live updates via
// the WebSocket "flightStatus" + "gsx" channels (already merged into
// AppStateContext by the WS hook).
export function FlightStatusPanel() {
    const { get } = useApi();
    const { state, dispatch } = useAppState();
    useEffect(() => {
        let cancelled = false;
        (async () => {
            try {
                const dto = await get("/flightstatus");
                if (!cancelled)
                    dispatch({ type: "set", channel: "flightStatus", state: dto });
            }
            catch {
                /* useApi already handled 401; transient errors are fine — WS will fill in */
            }
        })();
        return () => { cancelled = true; };
    }, [get, dispatch]);
    const fs = state.flightStatus;
    if (!fs) {
        return _jsx("div", { className: styles.loading, children: "Loading flight status\u2026" });
    }
    return (_jsxs("div", { className: styles.panel, children: [_jsx(FlightPhaseBox, { phase: fs.gsx.appAutomationState }), _jsxs(Section, { title: "Sim", children: [_jsx(Indicator, { label: "Sim Running", ok: fs.simRunning }), _jsx(Indicator, { label: "Sim Connected", ok: fs.simConnected }), _jsx(Indicator, { label: "Sim Session", ok: fs.simSession }), _jsx(Indicator, { label: "Sim Paused", ok: !fs.simPaused, reverseLabel: "Paused" }), _jsx(Indicator, { label: "Walkaround", ok: !fs.simWalkaround, reverseLabel: "Active" }), _jsx(KV, { label: "Camera State", value: String(fs.cameraState) }), _jsx(KV, { label: "Sim Version", value: fs.simVersion || "—" }), _jsx(KV, { label: "Aircraft", value: fs.aircraftString || "—" })] }), _jsxs(Section, { title: "App", children: [_jsx(Indicator, { label: "GSX Controller", ok: fs.appGsxController }), _jsx(Indicator, { label: "ProSim Binary", ok: fs.appAircraftBinary }), _jsx(Indicator, { label: "Aircraft Interface", ok: fs.appAircraftInterface }), _jsx(Indicator, { label: "ProSim SDK Connected", ok: fs.appProsimSdkConnected }), _jsx(Indicator, { label: "Automation Controller", ok: fs.appAutomationController }), _jsx(Indicator, { label: "Audio Controller", ok: fs.appAudioController }), _jsx(KV, { label: "Profile", value: fs.appProfile || "—" }), _jsx(KV, { label: "Aircraft", value: fs.appAircraft }), _jsx(KV, { label: "On Ground", value: String(fs.appOnGround) }), _jsx(KV, { label: "Engines Running", value: String(fs.appEnginesRunning) }), _jsx(KV, { label: "In Motion", value: String(fs.appInMotion) })] }), _jsxs(Section, { title: "GSX", children: [_jsx(Indicator, { label: "GSX Running", ok: fs.gsx.gsxRunning }), _jsx(Indicator, { label: "Couatl Vars", ok: fs.gsx.gsxStartedValid }), _jsx(KV, { label: "Couatl Started", value: fs.gsx.gsxStarted }), _jsx(KV, { label: "Menu State", value: fs.gsx.gsxMenu }), _jsx(KV, { label: "Pax Target", value: String(fs.gsx.gsxPaxTarget) }), _jsx(KV, { label: "Pax Total (B|D)", value: fs.gsx.gsxPaxTotal }), _jsx(KV, { label: "Cargo (B|D)", value: fs.gsx.gsxCargoProgress }), _jsx(KV, { label: "Departure Services", value: fs.gsx.appAutomationDepartureServices }), _jsx(KV, { label: "Assigned Gate", value: fs.gsx.assignedArrivalGate || "—" }), _jsx(KV, { label: "Last Handler Event", value: fs.gsx.lastHandlerEvent || "—" })] }), _jsxs(Section, { title: "Services", children: [_jsx(ServiceRow, { label: "Reposition", state: fs.gsx.serviceReposition }), _jsx(ServiceRow, { label: "Refuel", state: fs.gsx.serviceRefuel }), _jsx(ServiceRow, { label: "Catering", state: fs.gsx.serviceCatering }), _jsx(ServiceRow, { label: "Lavatory", state: fs.gsx.serviceLavatory }), _jsx(ServiceRow, { label: "Water", state: fs.gsx.serviceWater }), _jsx(ServiceRow, { label: "Cleaning", state: fs.gsx.serviceCleaning }), _jsx(GpuRow, { connected: fs.gsx.serviceGpuConnected, relevant: fs.gsx.serviceGpuPhaseRelevant }), _jsx(ServiceRow, { label: "Boarding", state: fs.gsx.serviceBoarding }), _jsx(ServiceRow, { label: "Deboarding", state: fs.gsx.serviceDeboarding }), _jsx(ServiceRow, { label: "Pushback", state: parsePushbackState(fs.gsx.servicePushback) }), _jsx(ServiceRow, { label: "Jetway", state: fs.gsx.serviceJetway }), _jsx(ConnectedRow, { label: "Jetway Connected", connected: fs.gsx.serviceJetwayConnected }), _jsx(ServiceRow, { label: "Stairs", state: fs.gsx.serviceStairs }), _jsx(ConnectedRow, { label: "Stairs Connected", connected: fs.gsx.serviceStairsConnected })] }), _jsx(Section, { title: "Deice Holdover (HOT)", children: _jsx(HotCard, { hot: fs.deiceHoldover }) }), _jsx(Section, { title: "Log", children: _jsx(LogTail, { messages: fs.messageLog }) })] }));
}
const PHASE_BLOCKS = [
    { label: "PREFLIGHT", states: ["SessionStart", "Preparation"] },
    { label: "DEPARTURE", states: ["Departure"] },
    { label: "PUSHBACK", states: ["PushBack"] },
    { label: "TAXI OUT", states: ["TaxiOut"] },
    { label: "FLIGHT", states: ["Flight"] },
    { label: "TAXI IN", states: ["TaxiIn"] },
    { label: "ARRIVAL", states: ["Arrival", "TurnAround"] },
];
function FlightPhaseBox({ phase }) {
    return (_jsxs("section", { className: `${styles.section} ${styles.phaseSection}`, children: [_jsx("h3", { className: styles.sectionTitle, children: "Flight Phase" }), _jsxs("div", { className: styles.phaseBody, children: [_jsx("div", { className: styles.phaseLabel, children: phase }), _jsxs("div", { className: styles.phaseGrid, children: [PHASE_BLOCKS.map((b) => (_jsx("div", { className: `${styles.phaseBlock} ${b.states.includes(phase) ? styles.phaseBlockActive : ""}` }, b.label))), PHASE_BLOCKS.map((b) => (_jsx("div", { className: styles.phaseBlockLabel, children: b.label }, `${b.label}-l`)))] })] })] }));
}
const PRECIP_OPTIONS = [
    { value: "None", label: "No precipitation" },
    { value: "ActiveFrost", label: "Active frost" },
    { value: "FreezingFog", label: "Freezing fog" },
    { value: "Snow", label: "Snow / snow grains" },
    { value: "FreezingDrizzleLight", label: "Freezing drizzle (light)" },
    { value: "FreezingDrizzleModerate", label: "Freezing drizzle (moderate)" },
    { value: "LightFreezingRain", label: "Light freezing rain" },
    { value: "RainOnColdSoakedWing", label: "Rain on cold-soaked wing" },
];
function fmtMMSS(total) {
    const m = Math.floor((total ?? 0) / 60);
    const s = (total ?? 0) % 60;
    return `${m}:${String(s).padStart(2, "0")}`;
}
// Deice holdover card. Precip + OAT are crew inputs POSTed to /api/deice;
// the server recomputes and the result rides back on the deiceHoldover WS
// channel, so we don't optimistically mutate local state.
function HotCard({ hot }) {
    const { post } = useApi();
    const [oatText, setOatText] = useState(String(hot?.oatC ?? 0));
    const oatFocused = useRef(false);
    // Keep the field in sync with the server (auto-prefill / other clients)
    // unless the user is actively editing it.
    useEffect(() => {
        if (!oatFocused.current)
            setOatText(String(hot?.oatC ?? 0));
    }, [hot?.oatC]);
    if (!hot) {
        return (_jsx("div", { className: styles.row, children: _jsx("span", { className: styles.rowLabel, children: "\u2014" }) }));
    }
    const changePrecip = async (precip) => {
        try {
            await post("/deice/set-precip", { precip });
        }
        catch {
            /* WS will reconcile */
        }
    };
    const commitOat = async () => {
        oatFocused.current = false;
        const v = parseFloat(oatText);
        if (Number.isFinite(v)) {
            try {
                await post("/deice/set-oat", { oatC: v });
            }
            catch {
                /* WS will reconcile */
            }
        }
    };
    return (_jsxs(_Fragment, { children: [_jsxs("div", { className: styles.row, children: [_jsx("span", { className: styles.rowLabel, children: "Precipitation" }), _jsx("span", { className: styles.rowValue, children: _jsx("select", { className: styles.hotSelect, value: hot.precip, onChange: (e) => changePrecip(e.target.value), children: PRECIP_OPTIONS.map((o) => (_jsx("option", { value: o.value, children: o.label }, o.value))) }) })] }), _jsxs("div", { className: styles.row, children: [_jsxs("span", { className: styles.rowLabel, children: ["OAT (\u00B0C)", hot.oatUserSet ? "" : " · auto"] }), _jsx("span", { className: styles.rowValue, children: _jsx("input", { className: styles.hotInput, type: "number", step: "0.5", value: oatText, onFocus: () => {
                                oatFocused.current = true;
                            }, onChange: (e) => setOatText(e.target.value), onBlur: commitOat, onKeyDown: (e) => {
                                if (e.key === "Enter")
                                    commitOat();
                            } }) })] }), _jsx(KV, { label: "Fluid", value: hot.fluidLabel || "—" }), hot.active && (_jsxs("div", { className: styles.row, children: [_jsx("span", { className: styles.rowLabel, children: "Holdover remaining" }), _jsxs("span", { className: styles.hotCountdown, children: [fmtMMSS(hot.remainingLowSeconds), " \u2013 ", fmtMMSS(hot.remainingHighSeconds)] })] })), _jsxs("div", { className: styles.row, children: [_jsx("span", { className: styles.rowLabel, children: "Status" }), _jsx("span", { className: hot.expired ? styles.hotExpired : styles.rowValue, children: hot.status || (hot.fluidLabel ? "—" : "Awaiting GSX deicing…") })] }), _jsx("p", { className: styles.hotDisclaimer, children: "Representative HOT figures for simulation immersion only \u2014 not a certified table. Do not use for real-world dispatch." })] }));
}
function Section({ title, children }) {
    return (_jsxs("section", { className: styles.section, children: [_jsx("h3", { className: styles.sectionTitle, children: title }), _jsx("div", { className: styles.sectionBody, children: children })] }));
}
function Indicator({ label, ok, reverseLabel }) {
    return (_jsxs("div", { className: styles.row, children: [_jsx("span", { className: styles.rowLabel, children: label }), _jsx("span", { className: `${styles.dot} ${ok ? styles.dotOk : styles.dotBad}`, "aria-hidden": "true" }), _jsx("span", { className: styles.rowValue, children: ok ? "OK" : reverseLabel ?? "—" })] }));
}
function KV({ label, value }) {
    return (_jsxs("div", { className: styles.row, children: [_jsx("span", { className: styles.rowLabel, children: label }), _jsx("span", { className: styles.rowValueMono, children: value })] }));
}
function ServiceRow({ label, state }) {
    const tone = serviceTone(state);
    return (_jsxs("div", { className: styles.row, children: [_jsx("span", { className: styles.rowLabel, children: label }), _jsx("span", { className: `${styles.pill} ${styles[`tone_${tone}`]}`, children: state })] }));
}
function GpuRow({ connected, relevant }) {
    // Grey when phase isn't relevant (Preparation/Departure/Arrival/TurnAround).
    const tone = !relevant ? "neutral" : connected ? "ok" : "bad";
    return (_jsxs("div", { className: styles.row, children: [_jsx("span", { className: styles.rowLabel, children: "GPU" }), _jsx("span", { className: `${styles.pill} ${styles[`tone_${tone}`]}`, children: !relevant ? "—" : connected ? "Connected" : "Disconnected" })] }));
}
function ConnectedRow({ label, connected }) {
    const tone = connected ? "ok" : "neutral";
    return (_jsxs("div", { className: styles.row, children: [_jsx("span", { className: styles.rowLabel, children: label }), _jsx("span", { className: `${styles.pill} ${styles[`tone_${tone}`]}`, children: connected ? "Connected" : "—" })] }));
}
// Pushback arrives as "{State} ({PushStatus})" (e.g. "Unknown (0)") because
// the WPF Monitor view shows the push-status count alongside the state. The
// web Services list only renders the state pill, so peel the leading word.
function parsePushbackState(value) {
    const head = value?.split(" ", 1)[0] ?? "";
    return (head || "Unknown");
}
function serviceTone(s) {
    switch (s) {
        case "Completed": return "ok";
        case "Active": return "active";
        case "Requested": return "warn";
        case "Callable": return "neutral";
        case "Bypassed":
        case "NotAvailable": return "neutral";
        case "Unknown":
        default: return "bad";
    }
}
function LogTail({ messages }) {
    // Auto-scroll to bottom on new messages, but only when the user hasn't
    // scrolled up to read older entries.
    const ref = useRef(null);
    const stickRef = useRef(true);
    useEffect(() => {
        const el = ref.current;
        if (!el)
            return;
        if (stickRef.current)
            el.scrollTop = el.scrollHeight;
    }, [messages]);
    function handleScroll() {
        const el = ref.current;
        if (!el)
            return;
        const threshold = 24;
        stickRef.current = el.scrollTop + el.clientHeight + threshold >= el.scrollHeight;
    }
    return (_jsx("div", { className: styles.log, ref: ref, onScroll: handleScroll, children: messages.length === 0 ? (_jsx("div", { className: styles.logEmpty, children: "No log entries yet." })) : (messages.map((m, i) => (_jsx("div", { className: styles.logLine, children: m }, i)))) }));
}
