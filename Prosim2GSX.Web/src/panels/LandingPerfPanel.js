import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useCallback, useEffect, useRef } from "react";
import { useApi } from "../api/useApi";
import { calculateLanding, getLanding, loadLandingRunways, postLandingInputs, resetLanding, } from "../api/perf";
import { useAppState } from "../state/AppStateContext";
import { KeyboardNumberInput } from "./perf-shared/KeyboardNumberInput";
import { MetarStrip } from "./perf-shared/MetarStrip";
import { RunwayDropdown } from "./perf-shared/RunwayDropdown";
import sharedStyles from "./perf-shared/PerfShared.module.css";
import styles from "./LandingPerfPanel.module.css";
// EFB-style LANDING performance tab.
//
// Per D5 this tab AUTO-recalculates: any input change is debounced
// 500 ms, then POSTed to /perf/landing/inputs followed immediately by
// /perf/landing/calculate. There's no explicit Calculate button —
// the result panel just refreshes. (Takeoff is the explicit-button
// path; landing is cheap enough server-side to recompute on change.)
//
// LDA / LDR / LDR+15 / HW / XW are all server-derived; the panel just
// renders them with the colour classes the service computed
// (hwClass / xwClass / visualDistClass) so the WPF + web surfaces
// read identically.
const DEBOUNCE_MS = 500;
// Runway-condition strip. Codes are fixed by the gateway: 6=Dry …
// 1=Poor. Displayed Dry → Poor (descending code) to match the EFB.
const SURFACE_OPTIONS = [
    { code: 6, label: "Dry" },
    { code: 5, label: "Good" },
    { code: 4, label: "G/M" },
    { code: 3, label: "Med" },
    { code: 2, label: "M/P" },
    { code: 1, label: "Poor" },
];
export function LandingPerfPanel() {
    const api = useApi();
    const { state, dispatch } = useAppState();
    const ld = state.landingPerf;
    const pendingInputs = useRef({});
    const flushTimer = useRef(null);
    useEffect(() => {
        let cancelled = false;
        (async () => {
            try {
                const dto = await getLanding(api);
                if (!cancelled)
                    dispatch({ type: "set", channel: "landingPerf", state: dto });
            }
            catch {
                /* WS will fill in once connected */
            }
        })();
        return () => { cancelled = true; };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);
    // D5 auto-recalc: flush pending inputs, then immediately trigger a
    // calculate. Both responses replace the channel; we dispatch the
    // calculate result last so the panel shows the recomputed output.
    const flushPending = useCallback(async () => {
        const payload = { ...pendingInputs.current };
        pendingInputs.current = {};
        flushTimer.current = null;
        if (Object.keys(payload).length === 0)
            return;
        try {
            await postLandingInputs(api, payload);
            const dto = await calculateLanding(api);
            dispatch({ type: "set", channel: "landingPerf", state: dto });
        }
        catch {
            /* WS will resync; server-side errors land on state.landingPerf.lastError */
        }
    }, [api, dispatch]);
    const setField = useCallback((key, value) => {
        pendingInputs.current[key] = value;
        if (flushTimer.current !== null)
            window.clearTimeout(flushTimer.current);
        flushTimer.current = window.setTimeout(flushPending, DEBOUNCE_MS);
    }, [flushPending]);
    useEffect(() => () => {
        if (flushTimer.current !== null) {
            window.clearTimeout(flushTimer.current);
            flushTimer.current = null;
            void flushPending();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);
    if (!ld) {
        return _jsx("div", { className: `${sharedStyles.scope} ${styles.loading}`, children: "Loading landing performance\u2026" });
    }
    const onIcaoCommit = (raw) => {
        const icao = raw.toUpperCase().trim().slice(0, 4);
        setField("icao", icao);
        if (icao.length === 4) {
            (async () => {
                try {
                    await loadLandingRunways(api, icao);
                    const dto = await calculateLanding(api);
                    dispatch({ type: "set", channel: "landingPerf", state: dto });
                }
                catch { /* surfaced on next WS snapshot */ }
            })();
        }
    };
    const onReset = async () => {
        if (!window.confirm("Reset all landing perf inputs?"))
            return;
        try {
            const dto = await resetLanding(api);
            dispatch({ type: "set", channel: "landingPerf", state: dto });
        }
        catch { /* WS will resync */ }
    };
    // HW label flips on sign; abs value rendered. Server supplies the
    // colour class so WPF + web read identically.
    const hwSign = ld.hwKt < 0 ? "TW (KT)" : "HW (KT)";
    const hwAbs = Math.round(Math.abs(ld.hwKt));
    const xwAbs = Math.round(Math.abs(ld.xwKt));
    const showResult = ld.hasResult && !ld.isNoData && !ld.retreatFlap;
    return (_jsxs("div", { className: `${sharedStyles.scope} ${styles.panel}`, children: [_jsxs("div", { className: styles.header, children: [_jsx("span", { className: styles.title, children: "LANDING PERFORMANCE" }), _jsx("span", { className: styles.flightInfo, children: ld.icao || "----" })] }), _jsxs("div", { className: styles.body, children: [_jsxs("div", { className: styles.column, children: [_jsx("div", { className: styles.columnTitle, children: "Airport / Runway" }), _jsx(FieldRow, { label: "ICAO", children: _jsx("input", { type: "text", className: styles.icaoInput, defaultValue: ld.icao, maxLength: 4, onBlur: (e) => onIcaoCommit(e.target.value), onKeyDown: (e) => {
                                        if (e.key === "Enter")
                                            e.target.blur();
                                    } }, ld.icao) }), _jsx(FieldRow, { label: "Runway", children: _jsx(RunwayDropdown, { runways: ld.runways, runwayId: ld.runwayId, onRunwayChange: (rwy) => setField("runwayId", rwy) }) }), _jsx("div", { className: styles.columnTitle, style: { marginTop: 12 }, children: "Runway Condition" }), _jsx("div", { className: styles.surfaceStrip, children: SURFACE_OPTIONS.map((s) => (_jsx("button", { type: "button", className: `${styles.surfaceBtn} ${s.code === ld.rwySurfaceCode ? styles.active : ""}`, onClick: () => setField("rwySurfaceCode", s.code), children: s.label }, s.code))) }), _jsx("div", { className: styles.columnTitle, style: { marginTop: 12 }, children: "Wind / Weather" }), _jsx(FieldRow, { label: "Wind", children: _jsxs("div", { className: styles.windInputRow, children: [_jsx("input", { type: "text", defaultValue: ld.windDir, onBlur: (e) => setField("windDir", e.target.value.toUpperCase().slice(0, 3)), maxLength: 3, "aria-label": "Wind direction" }), _jsx("span", { style: { color: "var(--efb-text-dim)" }, children: "/" }), _jsx(KeyboardNumberInput, { value: ld.windKt, onCommit: (n) => setField("windKt", n), unit: "kt", integer: true, width: 3, min: 0, ariaLabel: "Wind speed" })] }) }), _jsx(FieldRow, { label: "OAT", children: _jsx(KeyboardNumberInput, { value: ld.oatC, onCommit: (n) => setField("oatC", n), unit: "\u00B0C", integer: true, width: 4, ariaLabel: "OAT" }) }), _jsx(FieldRow, { label: "QNH", children: _jsx(KeyboardNumberInput, { value: ld.qnhHpa, onCommit: (n) => setField("qnhHpa", n), unit: "hPa", integer: true, width: 5, min: 900, max: 1100, ariaLabel: "QNH" }) })] }), _jsxs("div", { className: styles.column, children: [_jsx("div", { className: styles.columnTitle, children: "Aircraft Config" }), _jsx(FieldRow, { label: "Ldg weight", children: _jsx(KeyboardNumberInput, { value: ld.ldgWeightTons, onCommit: (n) => setField("ldgWeightTons", n), unit: "t", width: 5, min: 0, ariaLabel: "Landing weight" }) }), _jsx(FieldRow, { label: "Flap", children: _jsx(ToggleGroup, { value: ld.flapConfig, options: ["FULL", "3"], onChange: (v) => setField("flapConfig", v) }) }), _jsx(FieldRow, { label: "Brake", children: _jsx(ToggleGroup, { value: ld.brakeMode, options: ["LOW", "MED", "MAX"], onChange: (v) => setField("brakeMode", v) }) }), _jsx(FieldRow, { label: "Reverse", children: _jsx(ToggleGroup, { value: ld.revMode, options: ["idle", "max"], onChange: (v) => setField("revMode", v) }) }), _jsx(FieldRow, { label: "Autoland", children: _jsx(ToggleGroup, { value: ld.autolandMode, options: ["manual", "auto"], onChange: (v) => setField("autolandMode", v) }) }), _jsx(FieldRow, { label: "A/THR", children: _jsx(ToggleGroup, { value: ld.athr, options: ["0", "1"], onChange: (v) => setField("athr", v) }) }), _jsx(FieldRow, { label: "VAPP override", children: _jsx(KeyboardNumberInput, { value: ld.aircraftSpeedKt ?? 0, onCommit: (n) => setField("aircraftSpeedKt", n > 0 ? n : undefined), unit: "kt", integer: true, width: 4, min: 0, ariaLabel: "VAPP override" }) })] }), _jsxs("div", { className: styles.column, children: [_jsx("div", { className: styles.columnTitle, children: "Output" }), ld.isNoData && (_jsx("div", { className: `${styles.banner} ${styles.bannerWarn}`, children: "NO PERFORMANCE DATA" })), ld.retreatFlap && (_jsx("div", { className: `${styles.banner} ${styles.bannerWarn}`, children: "RETREAT FLAP CONFIG" })), _jsxs("div", { className: styles.outFrame, children: [_jsxs("div", { className: styles.outRow, children: [_jsx("span", { className: styles.outLabel, children: "LDR" }), _jsxs("span", { children: [_jsx("span", { className: `${styles.outValue} ${styles.outValueBig} ${valClass(ld.visualDistClass)}`, children: showResult ? ld.ldrM : "----" }), _jsx("span", { className: styles.outUnit, children: "m" })] })] }), _jsxs("div", { className: styles.outRow, children: [_jsx("span", { className: styles.outLabel, children: "LDR + 15%" }), _jsxs("span", { children: [_jsx("span", { className: `${styles.outValue} ${valClass(ld.visualDistClass)}`, children: showResult ? ld.ldr15M : "----" }), _jsx("span", { className: styles.outUnit, children: "m" })] })] }), _jsxs("div", { className: styles.outRow, children: [_jsx("span", { className: styles.outLabel, children: "LDA" }), _jsxs("span", { children: [_jsx("span", { className: `${styles.outValue} ${valClass(ld.visualDistClass)}`, children: ld.ldaM > 0 ? Math.round(ld.ldaM) : "----" }), _jsx("span", { className: styles.outUnit, children: "m" })] })] }), _jsxs("div", { className: styles.outRow, children: [_jsx("span", { className: styles.outLabel, children: hwSign }), _jsx("span", { className: `${styles.outValue} ${valClass(ld.hwClass)}`, children: showResult ? hwAbs : "----" })] }), _jsxs("div", { className: styles.outRow, children: [_jsx("span", { className: styles.outLabel, children: "XW (KT)" }), _jsx("span", { className: `${styles.outValue} ${valClass(ld.xwClass)}`, children: showResult ? xwAbs : "----" })] })] })] })] }), _jsxs("div", { className: styles.footer, children: [_jsx(MetarStrip, { icao: ld.icao, metarText: ld.metarText, fetchedAt: ld.metarFetchedAt }), ld.lastError && _jsx("div", { className: styles.errorRow, children: ld.lastError }), _jsxs("div", { className: styles.actions, children: [_jsx("button", { className: styles.actionBtn, disabled: true, title: "Failure cases arrive in a future release", children: "Failures\u2026" }), _jsx("button", { className: `${styles.actionBtn} ${styles.actionFlex}`, onClick: onReset, disabled: ld.isBusy, children: "Reset" }), ld.isBusy && _jsx("span", { className: styles.busy, children: "Working\u2026" })] })] })] }));
}
// ─────────────────────────────────────────────────────────────────────
function FieldRow({ label, children }) {
    return (_jsxs("div", { className: styles.fieldRow, children: [_jsx("span", { className: styles.fieldLabel, children: label }), _jsx("span", { className: styles.fieldValue, children: children })] }));
}
function ToggleGroup(props) {
    return (_jsx("div", { className: styles.toggleRow, children: props.options.map((opt) => (_jsx("button", { type: "button", className: `${styles.toggleBtn} ${opt === props.value ? styles.active : ""}`, onClick: () => props.onChange(opt), children: opt }, opt))) }));
}
// Maps the server-supplied colour code to a CSS module class.
function valClass(code) {
    if (code === "red")
        return styles.valRed;
    if (code === "red-margin")
        return styles.valRedMargin;
    return styles.valNormal;
}
