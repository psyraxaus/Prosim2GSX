import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useCallback, useEffect, useRef, useState } from "react";
import { useApi } from "../api/useApi";
import { calculateTakeoff, getTakeoff, loadTakeoffRunways, postTakeoffInputs, resetTakeoff, syncTakeoffLoadsheet, uplinkTakeoff, } from "../api/perf";
import { useAppState } from "../state/AppStateContext";
import { KeyboardNumberInput } from "./perf-shared/KeyboardNumberInput";
import { MetarStrip } from "./perf-shared/MetarStrip";
import { RunwayDropdown } from "./perf-shared/RunwayDropdown";
import sharedStyles from "./perf-shared/PerfShared.module.css";
import styles from "./TakeoffPerfPanel.module.css";
// EFB-style TAKEOFF performance tab.
//
// Data flow: initial REST fetch seeds state.takeoffPerf; subsequent
// mutations route through POST /api/perf/takeoff/inputs (debounced
// 300 ms) and POST /calculate / /uplink / /reset / /sync-loadsheet /
// /load-runways. The server broadcasts a `snapshot` envelope on every
// store change, which lands as a state-`set` and replaces the channel
// wholesale — no merge bookkeeping on our side.
//
// Per D5: explicit Calculate button (no auto-recalc). The Send Uplink
// button writes V1/VR/V2/FLAPS/FLEX/THS/SHIFT into the FMS PERF page
// via the server's dataref-write path.
const DEBOUNCE_MS = 300;
const UPLINK_BADGE_MS = 5000;
export function TakeoffPerfPanel() {
    const api = useApi();
    const { state, dispatch } = useAppState();
    const to = state.takeoffPerf;
    const ls = state.loadsheet;
    const [uplinkBadgeUntil, setUplinkBadgeUntil] = useState(0);
    const pendingInputs = useRef({});
    const flushTimer = useRef(null);
    // Initial REST fetch — seeds the channel so the panel can render
    // immediately even before the WS snapshot arrives.
    useEffect(() => {
        let cancelled = false;
        (async () => {
            try {
                const dto = await getTakeoff(api);
                if (!cancelled)
                    dispatch({ type: "set", channel: "takeoffPerf", state: dto });
            }
            catch {
                /* WS will fill in once connected */
            }
        })();
        return () => { cancelled = true; };
        // useApi returns stable refs, dispatch is stable too — empty deps fine.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);
    // Debounced /inputs flush. Each setField call merges into pendingInputs
    // and (re)arms a 300 ms timer; on fire it POSTs the whole pending object,
    // dispatches the response (which replaces the channel), and clears the
    // buffer.
    const flushPending = useCallback(async () => {
        const payload = { ...pendingInputs.current };
        pendingInputs.current = {};
        flushTimer.current = null;
        if (Object.keys(payload).length === 0)
            return;
        try {
            const dto = await postTakeoffInputs(api, payload);
            dispatch({ type: "set", channel: "takeoffPerf", state: dto });
        }
        catch {
            /* WS will resync; surfaced via state.takeoffPerf.lastError if server-side */
        }
    }, [api, dispatch]);
    const setField = useCallback((key, value) => {
        pendingInputs.current[key] = value;
        if (flushTimer.current !== null)
            window.clearTimeout(flushTimer.current);
        flushTimer.current = window.setTimeout(flushPending, DEBOUNCE_MS);
    }, [flushPending]);
    // Unmount: flush any pending edits so we don't drop the user's
    // last keystroke.
    useEffect(() => () => {
        if (flushTimer.current !== null) {
            window.clearTimeout(flushTimer.current);
            flushTimer.current = null;
            void flushPending();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);
    // Uplink badge auto-reset. The server's IsUplinked flag stays true
    // until any input changes (server-side invalidation), but the panel
    // adds a 5-second visual flash on top so the user sees the
    // confirmation moment even when they don't immediately edit anything.
    useEffect(() => {
        if (!to?.isUplinked)
            return;
        setUplinkBadgeUntil(Date.now() + UPLINK_BADGE_MS);
    }, [to?.isUplinked, to?.uplinkedAt]);
    useEffect(() => {
        if (uplinkBadgeUntil === 0)
            return;
        const remaining = uplinkBadgeUntil - Date.now();
        if (remaining <= 0)
            return;
        const id = window.setTimeout(() => setUplinkBadgeUntil(0), remaining);
        return () => window.clearTimeout(id);
    }, [uplinkBadgeUntil]);
    if (!to) {
        return _jsx("div", { className: `${sharedStyles.scope} ${styles.loading}`, children: "Loading takeoff performance\u2026" });
    }
    const hasLoadsheet = (ls?.prelim?.status === "received") ||
        (ls?.final?.status === "received");
    const canCalculate = !!to.runwayId && to.towKg > 0 && to.mactowPercent > 0 && !to.isBusy;
    const canUplink = to.hasResult && to.v1 > 0 && to.vr > 0 && to.v2 > 0 && !to.isBusy;
    const uplinkActive = uplinkBadgeUntil > 0 && uplinkBadgeUntil > Date.now();
    const onIcaoCommit = (raw) => {
        const icao = raw.toUpperCase().trim().slice(0, 4);
        setField("icao", icao);
        // ICAO change triggers an immediate runway load (which loads METAR too).
        if (icao.length === 4) {
            (async () => {
                try {
                    const dto = await loadTakeoffRunways(api, icao);
                    dispatch({ type: "set", channel: "takeoffPerf", state: dto });
                }
                catch { /* surfaced on next WS snapshot */ }
            })();
        }
    };
    const onCalculate = async () => {
        try {
            const dto = await calculateTakeoff(api);
            dispatch({ type: "set", channel: "takeoffPerf", state: dto });
        }
        catch { /* WS will resync */ }
    };
    const onSyncLoadsheet = async () => {
        try {
            const dto = await syncTakeoffLoadsheet(api);
            dispatch({ type: "set", channel: "takeoffPerf", state: dto });
        }
        catch { /* WS will resync */ }
    };
    const onUplink = async () => {
        try {
            const dto = await uplinkTakeoff(api);
            dispatch({ type: "set", channel: "takeoffPerf", state: dto });
            setUplinkBadgeUntil(Date.now() + UPLINK_BADGE_MS);
        }
        catch { /* error surface lives on the state's lastError */ }
    };
    const onReset = async () => {
        if (!window.confirm("Reset all takeoff perf inputs?"))
            return;
        try {
            const dto = await resetTakeoff(api);
            dispatch({ type: "set", channel: "takeoffPerf", state: dto });
        }
        catch { /* WS will resync */ }
    };
    // Headwind/tailwind label flips on sign.
    const hwSign = to.hwCompKt < 0 ? "TW" : "HW";
    const hwAbs = Math.abs(to.hwCompKt);
    return (_jsxs("div", { className: `${sharedStyles.scope} ${styles.panel}`, children: [_jsxs("div", { className: styles.header, children: [_jsx("span", { className: styles.title, children: "TAKEOFF PERFORMANCE" }), _jsxs("span", { className: styles.flightInfo, children: ["ENG ", to.engineVariant, " \u00B7 ", to.icao || "----"] })] }), _jsxs("div", { className: styles.body, children: [_jsxs("div", { className: styles.column, children: [_jsx("div", { className: styles.columnTitle, children: "Airport / Runway" }), _jsx(FieldRow, { label: "ICAO", children: _jsx("input", { type: "text", className: styles.icaoInput, defaultValue: to.icao, maxLength: 4, onBlur: (e) => onIcaoCommit(e.target.value), onKeyDown: (e) => {
                                        if (e.key === "Enter")
                                            e.target.blur();
                                    } }, to.icao) }), _jsx(FieldRow, { label: "Runway", children: _jsx(RunwayDropdown, { runways: to.runways, runwayId: to.runwayId, intersectionName: to.intersectionName, withIntersections: true, onRunwayChange: (rwy) => setField("runwayId", rwy), onIntersectionChange: (name) => setField("intersectionName", name) }) }), _jsx(FieldRow, { label: "Surface", children: _jsx(ToggleGroup, { value: to.surface, options: ["DRY", "WET"], onChange: (v) => setField("surface", v) }) }), _jsx("div", { className: styles.columnTitle, style: { marginTop: 12 }, children: "Wind / Weather" }), _jsx(FieldRow, { label: "Wind", children: _jsxs("div", { className: styles.windInputRow, children: [_jsx("input", { type: "text", defaultValue: to.windDir, onBlur: (e) => setField("windDir", e.target.value.toUpperCase().slice(0, 3)), maxLength: 3, "aria-label": "Wind direction" }), _jsx("span", { style: { color: "var(--efb-text-dim)" }, children: "/" }), _jsx(KeyboardNumberInput, { value: to.windKt, onCommit: (n) => setField("windKt", n), unit: "kt", integer: true, width: 3, min: 0, ariaLabel: "Wind speed" })] }) }), _jsx(FieldRow, { label: "OAT", children: _jsx(KeyboardNumberInput, { value: to.oatC, onCommit: (n) => setField("oatC", n), unit: "\u00B0C", integer: true, width: 4, ariaLabel: "OAT" }) }), _jsx(FieldRow, { label: "QNH", children: _jsx(KeyboardNumberInput, { value: to.qnhHpa, onCommit: (n) => setField("qnhHpa", n), unit: "hPa", integer: true, width: 5, min: 900, max: 1100, ariaLabel: "QNH" }) })] }), _jsxs("div", { className: styles.column, children: [_jsx("div", { className: styles.columnTitle, children: "Aircraft Config" }), _jsx(FieldRow, { label: "Flap", children: _jsx(ToggleGroup, { value: to.flap, options: ["opt", "1+F", "2", "3"], onChange: (v) => setField("flap", v) }) }), _jsx(FieldRow, { label: "Anti-ice", children: _jsx(ToggleGroup, { value: to.antiIce, options: ["OFF", "ENG", "ENG+WING"], onChange: (v) => setField("antiIce", v) }) }), _jsx(FieldRow, { label: "Packs", children: _jsx(ToggleGroup, { value: to.packs, options: ["OFF", "ON"], onChange: (v) => setField("packs", v) }) }), _jsx(FieldRow, { label: "Force TOGA", children: _jsx("label", { className: styles.checkRow, children: _jsx("input", { type: "checkbox", checked: to.forceToga, onChange: (e) => setField("forceToga", e.target.checked) }) }) }), _jsx("div", { className: styles.columnTitle, style: { marginTop: 12 }, children: "Weights" }), _jsx(FieldRow, { label: "TOW", children: _jsx(KeyboardNumberInput, { value: to.towKg, onCommit: (n) => setField("towKg", n), unit: "kg", integer: true, width: 7, min: 0, ariaLabel: "TOW" }) }), _jsx(FieldRow, { label: "MAC TOW", children: _jsx(KeyboardNumberInput, { value: to.mactowPercent, onCommit: (n) => setField("mactowPercent", n), unit: "%", width: 5, min: 0, ariaLabel: "MAC TOW" }) })] }), _jsxs("div", { className: styles.column, children: [_jsx("div", { className: styles.columnTitle, children: "Output" }), _jsxs("div", { className: styles.mcduFrame, children: [_jsx("div", { className: styles.mcduTitle, children: "FMGC TAKE OFF" }), _jsx(McduRow, { label: "V1", value: to.hasResult ? to.v1 : null }), _jsx(McduRow, { label: "VR", value: to.hasResult ? to.vr : null }), _jsx(McduRow, { label: "V2", value: to.hasResult ? to.v2 : null }), _jsx(McduRow, { label: "FLAPS", value: to.hasResult ? confLabel(to.flapSettings) : null }), _jsx(McduRow, { label: "FLEX", value: to.hasResult ? (to.flexOutputC === 0 ? "TOGA" : `${to.flexOutputC}°`) : null, amber: to.hasResult && to.flexOutputC === 0 }), _jsx(McduRow, { label: "THS", value: to.hasResult
                                            ? `${to.trimDir || (to.thsValue >= 0 ? "UP" : "DN")} ${Math.abs(to.thsValue).toFixed(1)}`
                                            : null }), _jsx(McduRow, { label: "SHIFT", value: to.hasResult ? `${to.shiftM}` : null })] }), _jsx("div", { className: styles.columnTitle, style: { marginTop: 12 }, children: "Limits" }), _jsx(FieldRow, { label: `${hwSign} component`, children: _jsx("span", { className: to.hasResult ? styles.mcduValue : styles.mcduValueDim, children: to.hasResult ? `${hwAbs} kt` : "----" }) }), _jsx(FieldRow, { label: "TOPL", children: _jsx("span", { className: to.toplLimited ? styles.mcduValueAmber : styles.mcduValue, children: to.hasResult ? `${Math.round(to.toplKg)} kg` : "----" }) }), to.greenDot !== null && (_jsx(FieldRow, { label: "Green dot", children: _jsxs("span", { className: styles.mcduValue, children: [to.greenDot, " kt"] }) }))] })] }), _jsxs("div", { className: styles.footer, children: [_jsx(MetarStrip, { icao: to.icao, metarText: to.metarText, fetchedAt: to.metarFetchedAt }), to.calculationError && (_jsx("div", { className: `${styles.banner} ${styles.bannerError}`, children: to.calculationError })), to.lastError && (_jsx("div", { className: styles.errorRow, children: to.lastError })), to.hasResult && to.toplLimited && (_jsx("div", { className: `${styles.banner} ${styles.bannerWarn}`, children: "*** TOPL LIMITED ***" })), to.hasResult && to.forceTogaResult && (_jsx("div", { className: `${styles.banner} ${styles.bannerWarn}`, children: "*** TOGA REQUIRED ***" })), _jsxs("div", { className: styles.actions, children: [hasLoadsheet && (_jsx("button", { className: styles.actionBtn, onClick: onSyncLoadsheet, disabled: to.isBusy, children: "Sync Loadsheet" })), _jsx("button", { className: `${styles.actionBtn} ${styles.actionPrimary}`, onClick: onCalculate, disabled: !canCalculate, children: "Calculate" }), _jsx("button", { className: `${styles.actionBtn} ${uplinkActive ? styles.actionSent : ""}`, onClick: onUplink, disabled: !canUplink || uplinkActive, children: uplinkActive ? "Uplink Sent" : "Send Uplink" }), _jsx("button", { className: `${styles.actionBtn} ${styles.actionFlex}`, onClick: onReset, disabled: to.isBusy, children: "Reset" }), to.isBusy && _jsx("span", { className: styles.busy, children: "Working\u2026" })] })] })] }));
}
// ─────────────────────────────────────────────────────────────────────
//  Small layout helpers (panel-local; the perf-shared module has the
//  cross-panel pieces).
// ─────────────────────────────────────────────────────────────────────
function FieldRow({ label, children }) {
    return (_jsxs("div", { className: styles.fieldRow, children: [_jsx("span", { className: styles.fieldLabel, children: label }), _jsx("span", { className: styles.fieldValue, children: children })] }));
}
function ToggleGroup(props) {
    return (_jsx("div", { className: styles.toggleRow, children: props.options.map((opt) => (_jsx("button", { type: "button", className: `${styles.toggleBtn} ${opt === props.value ? styles.active : ""}`, disabled: props.disabled, onClick: () => props.onChange(opt), children: opt }, opt))) }));
}
function McduRow({ label, value, amber }) {
    const cls = value === null
        ? styles.mcduValueDim
        : amber
            ? styles.mcduValueAmber
            : styles.mcduValue;
    return (_jsxs("div", { className: styles.mcduRow, children: [_jsx("span", { className: styles.mcduLabel, children: label }), _jsx("span", { className: cls, children: value === null ? "----" : value })] }));
}
function confLabel(flapSettings) {
    if (flapSettings === 1)
        return "1+F";
    if (flapSettings === 2)
        return "2";
    if (flapSettings === 3)
        return "3";
    return "—";
}
