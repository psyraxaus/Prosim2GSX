import { jsx as _jsx, jsxs as _jsxs, Fragment as _Fragment } from "react/jsx-runtime";
import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { useApi } from "../api/useApi";
import { useAppState } from "../state/AppStateContext";
import styles from "./WeightBalancePanel.module.css";
// Read-only Weight & Balance panel. Initial REST load on mount; live
// updates arrive through the WebSocket "weightBalance" channel and are
// merged into AppState by the default reducer branch.
//
// Layout system: every annotation, tick, label, and dot is positioned
// relative to the PNG image element's measured bounding box
// (offsetLeft / offsetTop / offsetWidth / offsetHeight). The PNG sizes
// itself naturally via CSS (width 80%, height auto, centred), and a
// ResizeObserver re-reads the bounding box whenever the chart container
// changes size. Position formulas are simple ratios — e.g. MTOW sits at
// (left + 0.47 × width, top + 0.125 × height) — making the layout
// resolution-independent and identical at any container size.
// ── Axis ranges (A320 family, KG mode) ─────────────────────────────────
const TOP_NUM_MIN = 20;
const TOP_NUM_MAX = 39;
const LEFT_NUM_MIN_KG = 35;
const LEFT_NUM_MAX_KG = 78;
const LEFT_KG_STEP = 5;
// MAC% range used for clamping the live CG dots and the gauge-bar fills.
const AXIS_MAC_MIN = 21;
const AXIS_MAC_MAX = 38;
// ── Generated label / tick arrays ──────────────────────────────────────
// X-axis %MAC labels: skip the endpoint values (20, 39) and label every
// integer between. Yields [21, 22, …, 38].
const TOP_NUMBERS = Array(TOP_NUM_MAX - TOP_NUM_MIN - 1)
    .fill(0)
    .map((_, S) => TOP_NUM_MIN + S + 1);
// Y-axis weight labels at 5T intervals. Yields [35, 40, 45, …, 75].
const LEFT_NUMBERS_KG = Array(Math.round((LEFT_NUM_MAX_KG - LEFT_NUM_MIN_KG) / LEFT_KG_STEP))
    .fill(0)
    .map((_, S) => LEFT_NUM_MIN_KG + S * LEFT_KG_STEP);
// Y-axis tick marks at 1T intervals. Long when divisible by 5T (matches a
// numeric label), short otherwise. Yields [35, 36, …, 77].
const LEFT_LINES_KG = Array(Math.round(LEFT_NUM_MAX_KG - LEFT_NUM_MIN_KG))
    .fill(0)
    .map((_, S) => LEFT_NUM_MIN_KG + S);
// ── Annotation positions as ratios of PNG width / height ───────────────
const ANNOT_RATIOS = {
    mtow: { x: 0.47, y: 0.125 }, // MTOW = 73,500KG
    mlw: { x: 0.43, y: 0.29 }, // MLW  = 64,500KG
    mzfw: { x: 0.40, y: 0.37 }, // MZFW = 61,000KG
    opLim: { x: 0.40, y: 0.56 }, // Operational Limits (centre, horizontal)
    tolR: { x: 0.69, y: 0.56 }, // Take-Off Limits — right side, rotated
    zfwR: { x: 0.70, y: 0.66 }, // Zfw limit — right side, rotated
    zfwL: { x: 0.16, y: 0.64 }, // Zfw limit — left side, rotated
    tolL: { x: 0.12, y: 0.80 }, // Take-Off Limits — bottom-left, rotated
};
// Dot half-size in px. Fixed (the source chart uses a separate small
// element measured at runtime; ours is a constant for simplicity).
const DOT_RADIUS = 11;
export function WeightBalancePanel() {
    const { get, post } = useApi();
    const { state, dispatch } = useAppState();
    // Sync-to-FMS UI state. "idle" | "pending" | "success" | "error".
    // Success/error transitions are auto-cleared on a setTimeout matching
    // the WPF DispatcherTimer durations (3 s green / 5 s red).
    const [syncStatus, setSyncStatus] = useState("idle");
    const [syncMessage, setSyncMessage] = useState("");
    const syncTimerRef = useRef(null);
    // PNG image bounding box. All chart annotations are positioned relative
    // to these four values. Re-measured by ResizeObserver on container size
    // changes; also re-measured on the image's `load` event so the first
    // useful measurement happens as soon as the bitmap dimensions are known.
    const imgRef = useRef(null);
    const containerRef = useRef(null);
    const [pngBox, setPngBox] = useState({ left: 0, top: 0, width: 0, height: 0 });
    const measure = () => {
        const el = imgRef.current;
        if (!el || el.offsetHeight === 0)
            return;
        setPngBox({
            left: el.offsetLeft,
            top: el.offsetTop,
            width: el.offsetWidth,
            height: el.offsetHeight,
        });
    };
    useLayoutEffect(() => {
        measure();
        const ro = new ResizeObserver(() => measure());
        if (containerRef.current)
            ro.observe(containerRef.current);
        return () => ro.disconnect();
    }, []);
    useEffect(() => {
        let cancelled = false;
        (async () => {
            try {
                const dto = await get("/weightbalance");
                if (!cancelled) {
                    dispatch({
                        type: "set",
                        channel: "weightBalance",
                        state: dto,
                    });
                }
            }
            catch {
                /* useApi already handled 401; WS will fill in once connected */
            }
        })();
        return () => { cancelled = true; };
    }, [get, dispatch]);
    // Clear any pending flash timer on unmount so a navigate-away mid-flash
    // doesn't fire setState on an unmounted component.
    useEffect(() => () => {
        if (syncTimerRef.current !== null) {
            window.clearTimeout(syncTimerRef.current);
            syncTimerRef.current = null;
        }
    }, []);
    const wb = state.weightBalance;
    if (!wb) {
        return _jsx("div", { className: styles.loading, children: "Loading weight & balance\u2026" });
    }
    const triggerFlash = (status, message, durationMs) => {
        setSyncStatus(status);
        setSyncMessage(message);
        if (syncTimerRef.current !== null)
            window.clearTimeout(syncTimerRef.current);
        syncTimerRef.current = window.setTimeout(() => {
            setSyncStatus("idle");
            setSyncMessage("");
            syncTimerRef.current = null;
        }, durationMs);
    };
    const handleSync = async () => {
        if (syncStatus === "pending" || wb.macTowError)
            return;
        setSyncStatus("pending");
        setSyncMessage("");
        try {
            const result = await post("/fms/sync");
            if (result?.success) {
                triggerFlash("success", "FMS UPDATED", 3000);
            }
            else {
                triggerFlash("error", result?.errorMessage || "FMS sync failed", 5000);
            }
        }
        catch (e) {
            triggerFlash("error", e?.message || "FMS sync failed", 5000);
        }
    };
    // ── Coordinate helpers ─────────────────────────────────────────────────
    // X position for a MAC% value, anchored at the PNG's left edge.
    const xForMac = (m) => pngBox.left + (pngBox.width * (m - TOP_NUM_MIN)) / (TOP_NUM_MAX - TOP_NUM_MIN);
    // Y position for a weight (in tonnes), anchored at the PNG's top edge.
    // The −2 offset matches the source chart so labels visually align with
    // the centre of their tick marks.
    const yForT = (t) => pngBox.top + (pngBox.height * (LEFT_NUM_MAX_KG - t)) / (LEFT_NUM_MAX_KG - LEFT_NUM_MIN_KG) - 2;
    const annotPos = (key) => ({
        left: pngBox.left + ANNOT_RATIOS[key].x * pngBox.width,
        top: pngBox.top + ANNOT_RATIOS[key].y * pngBox.height,
    });
    // Map live DTO values to PNG-relative pixel positions for the dots.
    const macToX = (mac) => {
        if (!Number.isFinite(mac) || mac <= 0)
            return xForMac(AXIS_MAC_MIN);
        const clamped = Math.max(AXIS_MAC_MIN, Math.min(AXIS_MAC_MAX, mac));
        return xForMac(clamped);
    };
    const weightToY = (kg) => {
        const t = (kg ?? 0) / 1000;
        if (!Number.isFinite(t) || t <= 0)
            return yForT(LEFT_NUM_MIN_KG);
        const clamped = Math.max(LEFT_NUM_MIN_KG, Math.min(75, t));
        return yForT(clamped);
    };
    const macFraction = (mac) => {
        if (!Number.isFinite(mac) || mac <= 0)
            return 0;
        const clamped = Math.max(AXIS_MAC_MIN, Math.min(AXIS_MAC_MAX, mac));
        return (clamped - AXIS_MAC_MIN) / (AXIS_MAC_MAX - AXIS_MAC_MIN);
    };
    const zfwDotLeft = macToX(wb.maczfwPercent) - DOT_RADIUS;
    const zfwDotTop = weightToY(wb.zfwKg) - DOT_RADIUS;
    const gwDotLeft = macToX(wb.macgwPercent) - DOT_RADIUS;
    const gwDotTop = weightToY(wb.gwKg) - DOT_RADIUS;
    const zfwBarPct = macFraction(wb.maczfwPercent) * 100;
    const gwBarPct = macFraction(wb.macgwPercent) * 100;
    // Hide dots when there's no data (avoids parking them at a corner).
    const showZfwDot = wb.zfwKg > 0 && wb.maczfwPercent > 0;
    const showGwDot = wb.gwKg > 0 && wb.macgwPercent > 0;
    const totalCapacity = wb.zone1Capacity + wb.zone2Capacity + wb.zone3Capacity + wb.zone4Capacity;
    const cargoLoadedTotal = wb.cargoFwdLoadedKg + wb.cargoAftLoadedKg;
    // Suppress overlay rendering until the PNG box is measured; otherwise
    // labels would briefly stack at (0, 0) on first paint.
    const ready = pngBox.height > 0;
    return (_jsxs("div", { className: styles.panel, children: [_jsxs("section", { className: styles.chartCol, children: [_jsx("h2", { className: styles.colHeading, children: "CG Envelope" }), _jsxs("div", { className: styles.chartCard, children: [_jsxs("div", { ref: containerRef, className: styles.chart, children: [_jsx("img", { ref: imgRef, className: styles.chartImg, src: "/assets/img/wandb.png", alt: "", onLoad: measure }), ready && (_jsxs(_Fragment, { children: [LEFT_NUMBERS_KG.map((t) => (_jsx("div", { className: `${styles.axisLabel} ${styles.axisLabelY}`, style: { left: pngBox.left, top: yForT(t) }, children: t }, `ylbl-${t}`))), LEFT_LINES_KG.map((t) => {
                                                const isLong = t % 5 === 0;
                                                return (_jsx("div", { className: isLong ? styles.tickLong : styles.tickShort, style: { left: pngBox.left, top: yForT(t) } }, `tick-${t}`));
                                            }), _jsx("div", { className: `${styles.axisLabel} ${styles.axisLabelX}`, style: { left: pngBox.left - 10, top: pngBox.top - 10 }, children: "%MAC" }), TOP_NUMBERS.map((m) => (_jsx("div", { className: `${styles.axisLabel} ${styles.axisLabelX}`, style: { left: xForMac(m), top: pngBox.top - 10 }, children: m }, `xlbl-${m}`))), _jsx("div", { className: styles.limitLabel, style: annotPos("mtow"), children: "MTOW = 73,500KG" }), _jsx("div", { className: styles.limitLabel, style: annotPos("mlw"), children: "MLW = 64,500KG" }), _jsx("div", { className: styles.limitLabel, style: annotPos("mzfw"), children: "MZFW = 61,000KG" }), _jsx("div", { className: `${styles.envLabel} ${styles.annot1}`, style: annotPos("opLim"), children: "Operational Limits" }), _jsx("div", { className: `${styles.envLabel} ${styles.annot2}`, style: annotPos("tolR"), children: "Take-Off Limits" }), _jsx("div", { className: `${styles.envLabel} ${styles.annot3}`, style: annotPos("zfwR"), children: "Zfw limit" }), _jsx("div", { className: `${styles.envLabel} ${styles.annot4}`, style: annotPos("zfwL"), children: "Zfw limit" }), _jsx("div", { className: `${styles.envLabel} ${styles.annot5}`, style: annotPos("tolL"), children: "Take-Off Limits" }), showZfwDot && (_jsxs("svg", { className: styles.dot, style: { left: zfwDotLeft, top: zfwDotTop }, width: DOT_RADIUS * 2, height: DOT_RADIUS * 2, viewBox: "0 0 22 22", children: [_jsx("circle", { cx: "11", cy: "11", r: "10", fill: "#FFFFFF", stroke: "#000", strokeOpacity: "0.5" }), _jsx("path", { d: "M 11 1 A 10 10 0 0 1 21 11 L 11 11 Z", fill: "#E91E63" }), _jsx("path", { d: "M 11 21 A 10 10 0 0 1 1 11 L 11 11 Z", fill: "#E91E63" }), _jsx("line", { x1: "11", y1: "-2", x2: "11", y2: "24", stroke: "#FFFFFF", strokeWidth: "1.2" }), _jsx("line", { x1: "-2", y1: "11", x2: "24", y2: "11", stroke: "#FFFFFF", strokeWidth: "1.2" })] })), showGwDot && (_jsxs("svg", { className: styles.dot, style: { left: gwDotLeft, top: gwDotTop }, width: DOT_RADIUS * 2, height: DOT_RADIUS * 2, viewBox: "0 0 22 22", children: [_jsx("circle", { cx: "11", cy: "11", r: "10", fill: "#80B7CFD9", stroke: "#FFFFFF" }), _jsx("line", { x1: "11", y1: "-2", x2: "11", y2: "24", stroke: "#FFFFFF", strokeWidth: "1.2" }), _jsx("line", { x1: "-2", y1: "11", x2: "24", y2: "11", stroke: "#FFFFFF", strokeWidth: "1.2" })] }))] }))] }), _jsx("div", { className: styles.gaugeBar, children: _jsx("div", { className: styles.gaugeFillZfw, style: { width: `${zfwBarPct}%` } }) }), _jsx("div", { className: styles.gaugeBar, children: _jsx("div", { className: styles.gaugeFillGw, style: { width: `${gwBarPct}%` } }) }), _jsxs("table", { className: styles.summary, children: [_jsx("thead", { children: _jsxs("tr", { children: [_jsx("th", { children: "ZFW (KG)" }), _jsx("th", { children: "MACZFW (%)" }), _jsx("th", { children: "GW (KG)" }), _jsx("th", { children: "MACGW (%)" })] }) }), _jsx("tbody", { children: _jsxs("tr", { children: [_jsx("td", { children: wb.zfwKg.toLocaleString(undefined, { maximumFractionDigits: 0 }) }), _jsx("td", { children: wb.maczfwPercent.toFixed(1) }), _jsx("td", { children: wb.gwKg.toLocaleString(undefined, { maximumFractionDigits: 0 }) }), _jsx("td", { children: wb.macgwPercent.toFixed(1) })] }) })] }), _jsx("p", { className: styles.note, children: "NOTE: THE ABOVE FIGURES ARE LIVE." }), _jsxs("div", { className: styles.mactowRow, children: [_jsxs("div", { className: styles.mactowLine, children: [_jsx("span", { className: styles.mactowLabel, children: "MACTOW (%):" }), _jsx("span", { className: wb.macTowError ? styles.mactowValueError : styles.mactowValueOk, children: wb.mactowPercent.toFixed(1) }), wb.macTowError && (_jsx("span", { className: styles.mactowWarn, "aria-label": "out of range", children: "\u26A0" })), _jsx("span", { className: [
                                                    styles.sourceChip,
                                                    wb.macTowSource === "final" ? styles.sourceChipFinal : "",
                                                    wb.macTowSource === "prelim" ? styles.sourceChipPrelim : "",
                                                    wb.macTowSource === "computed" ? styles.sourceChipComputed : "",
                                                ].filter(Boolean).join(" "), title: wb.macTowSource === "final"
                                                    ? "MACTOW from final loadsheet (authoritative)"
                                                    : wb.macTowSource === "prelim"
                                                        ? "MACTOW from preliminary loadsheet (will upgrade when final arrives)"
                                                        : "No loadsheet received yet — value is live W&B computed mirror", children: wb.macTowSource === "final" ? "FINAL LS" : wb.macTowSource === "prelim" ? "PRELIM LS" : "COMPUTED" })] }), wb.macTowError && (_jsxs("div", { className: styles.mactowRange, children: ["VALID RANGE: ", wb.minMacTow.toFixed(1), " \u2013 ", wb.maxMacTow.toFixed(1)] })), wb.fmsSyncStale && (_jsxs("div", { className: styles.fmsStale, children: ["FMS OUT OF DATE", wb.fmsLastSyncedAt && (_jsxs(_Fragment, { children: [" \u2014 last sync ", new Date(wb.fmsLastSyncedAt).toLocaleTimeString(), " ", wb.fmsLastSyncedSource && `(${wb.fmsLastSyncedSource.toUpperCase()})`] }))] }))] }), _jsxs("div", { className: styles.fmsSyncRow, children: [_jsx("button", { type: "button", className: [
                                            styles.fmsSyncButton,
                                            syncStatus === "success" ? styles.fmsSyncSuccess : "",
                                            syncStatus === "error" ? styles.fmsSyncError : "",
                                        ].filter(Boolean).join(" "), disabled: syncStatus === "pending" || wb.macTowError, title: wb.macTowError ? "MACTOW out of range" : "", onClick: handleSync, children: syncStatus === "pending"
                                            ? "SYNCING…"
                                            : (() => {
                                                const verb = wb.fmsSyncStale ? "RESYNC TO FMS" : "SYNC TO FMS";
                                                const suffix = wb.macTowSource === "final" ? " (FINAL)" :
                                                    wb.macTowSource === "prelim" ? " (PRELIM)" :
                                                        " (COMPUTED)";
                                                return verb + suffix;
                                            })() }), syncMessage && (_jsx("span", { className: syncStatus === "success"
                                            ? styles.fmsSyncMessageOk
                                            : styles.fmsSyncMessageError, children: syncMessage }))] })] })] }), _jsxs("section", { className: styles.dataCol, children: [_jsx("h2", { className: styles.colHeading, children: "Passengers" }), _jsxs("div", { className: styles.dataCard, children: [_jsxs("div", { className: styles.capacity, children: ["CAPACITY: ", totalCapacity, " ECONOMY"] }), _jsxs("div", { className: styles.dataRow, children: [_jsx("span", { className: styles.label, children: "Pax" }), _jsx("span", { className: styles.headerCell, children: "PLANNED" }), _jsx("span", { className: styles.headerCell, children: "BOARDED" })] }), _jsxs("div", { className: styles.dataRow, children: [_jsx("span", { className: styles.label, children: "\u00A0" }), _jsx("span", { className: styles.value, children: wb.passengersPlanned }), _jsx("span", { className: styles.value, children: wb.passengersBoarded })] })] }), _jsx("h2", { className: styles.colHeading, children: "Cargo" }), _jsxs("div", { className: styles.dataCard, children: [_jsxs("div", { className: styles.capacity, children: ["CAPACITY:\u00A0", wb.cargoFwdCapacityKg.toLocaleString(), " KG FWD,\u00A0", wb.cargoAftCapacityKg.toLocaleString(), " KG AFT,\u00A0", wb.cargoBulkCapacityKg.toLocaleString(), " KG BULK"] }), _jsxs("div", { className: styles.dataRow, children: [_jsx("span", { className: styles.label, children: "Cargo" }), _jsx("span", { className: styles.headerCell, children: "PLANNED (KG)" }), _jsx("span", { className: styles.headerCell, children: "LOADED (KG)" })] }), _jsxs("div", { className: styles.dataRow, children: [_jsx("span", { className: styles.label, children: "\u00A0" }), _jsx("span", { className: styles.value, children: wb.cargoPlannedKg.toLocaleString(undefined, { maximumFractionDigits: 0 }) }), _jsx("span", { className: styles.value, children: cargoLoadedTotal.toLocaleString(undefined, { maximumFractionDigits: 0 }) })] }), _jsxs("div", { className: styles.dataRow, children: [_jsx("span", { className: styles.label, children: "FWD / AFT" }), _jsx("span", { className: styles.value, children: "\u00A0" }), _jsxs("span", { className: styles.subValue, children: [wb.cargoFwdLoadedKg.toLocaleString(undefined, { maximumFractionDigits: 0 }), " / ", wb.cargoAftLoadedKg.toLocaleString(undefined, { maximumFractionDigits: 0 })] })] })] }), _jsx("h2", { className: styles.colHeading, children: "Fuel" }), _jsxs("div", { className: styles.dataCard, children: [_jsxs("div", { className: styles.capacity, children: ["CAPACITY USABLE ", wb.fuelCapacityKg.toLocaleString(undefined, { maximumFractionDigits: 0 }), " KG \u2014 SG: 0.80"] }), _jsxs("div", { className: styles.dataRow, children: [_jsx("span", { className: styles.label, children: "Fuel" }), _jsx("span", { className: styles.headerCell, children: "PLANNED (KG)" }), _jsx("span", { className: styles.headerCell, children: "IN TANKS (KG)" })] }), _jsxs("div", { className: styles.dataRow, children: [_jsx("span", { className: styles.label, children: "\u00A0" }), _jsx("span", { className: styles.value, children: wb.fuelPlannedKg.toLocaleString(undefined, { maximumFractionDigits: 0 }) }), _jsx("span", { className: styles.value, children: wb.fuelInTanksKg.toLocaleString(undefined, { maximumFractionDigits: 0 }) })] })] })] })] }));
}
