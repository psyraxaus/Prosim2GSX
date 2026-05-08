import { jsx as _jsx, jsxs as _jsxs, Fragment as _Fragment } from "react/jsx-runtime";
import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { useApi } from "../api/useApi";
import { useAppState } from "../state/AppStateContext";
import { A320_OUTLINE_PATH } from "./a320Silhouette";
import styles from "./WeightBalancePanel.module.css";
// Aircraft Status section colours — matching the WPF model's brushes so
// the two surfaces read identically. Bulk on a non-fitted airframe falls
// back to a neutral grey; the rect is hidden, but the brush stays
// defined for the status text colour.
const DOOR_CLOSED_COLOR = "#4CAF50"; // green
const DOOR_OPEN_COLOR = "#F5A623"; // amber
const DOOR_NA_COLOR = "#555555"; // grey
// Seat overlay colours. Filled seats use the same green as a closed door
// for tonal consistency; empty seats use a darker neutral so the cabin
// reads as "empty by default" rather than "broken / N/A".
const SEAT_OCCUPIED_COLOR = "#4CAF50";
const SEAT_EMPTY_COLOR = "#2A2A2A";
function doorColor(open, fitted = true) {
    if (!fitted)
        return DOOR_NA_COLOR;
    return open ? DOOR_OPEN_COLOR : DOOR_CLOSED_COLOR;
}
function doorStatus(open, fitted = true) {
    if (!fitted)
        return "N/A";
    return open ? "OPEN" : "CLOSED";
}
// Parse the comma-separated seatOccupation string ("true,false,...") into
// a 132-bool array. Mirrors WeightBalanceService.CountTrueChars but keeps
// per-seat granularity for the cabin overlay. Tolerates the "1"/"0" shape
// some legacy producers used; case-insensitive.
function parseSeatOccupation(s) {
    if (!s)
        return [];
    return s.split(",").map(t => {
        const v = t.trim().toLowerCase();
        return v === "true" || v === "1";
    });
}
// ── Seat layout (source SVG coords, identical to door coords) ────────────
// Cabin tube runs source x=216..552 (aft → forward), y=355..395 (port →
// starboard) with the aisle at y=375. The -180° rotation around (375,
// 375) on the parent <g> flips this so the displayed cabin reads
// nose-LEFT, with port doors on the upper edge and starboard doors on
// the lower edge — same orientation the cargo doors already use.
//
// Zone breakdown is fixed by the A320 standard layout written into
// ProsimConstants.PaxZoneLimits {24, 30, 36, 42}. The seatOccupation
// string is indexed forward → aft, so seat index 0 is in zone 1 (most
// forward) and index 131 is in zone 4 (most aft). Each zone holds 6
// seats per row (3 port + aisle + 3 starboard).
const SEAT_ROWS_PER_ZONE = [4, 5, 6, 7]; // 24/6, 30/6, 36/6, 42/6 = 22 rows
const SEAT_ROW_PITCH = 15; // source-x distance between rows
const SEAT_RECT_W = 11; // source-x rect length
const SEAT_RECT_H = 4; // source-y rect height
const SEAT_X_FWD = 552; // source x of the forward-most row's leading edge
// Per-column source-y centres. A/B/C are port (low source y); D/E/F are
// starboard (high source y). Spacing leaves a visible aisle gap at y=375.
const SEAT_Y_BY_COL = [358, 364, 370, 380, 386, 392];
// Pre-computed per-seat rect coords. Index = global seat number 0..131.
// Computed once at module load so the render loop stays cheap.
const SEAT_RECTS = (() => {
    const out = [];
    let rowOffset = 0;
    for (let zone = 0; zone < SEAT_ROWS_PER_ZONE.length; zone++) {
        const rowsInZone = SEAT_ROWS_PER_ZONE[zone];
        for (let r = 0; r < rowsInZone; r++) {
            const globalRow = rowOffset + r;
            const x = SEAT_X_FWD - globalRow * SEAT_ROW_PITCH;
            for (let col = 0; col < 6; col++) {
                out.push({ x, y: SEAT_Y_BY_COL[col] - SEAT_RECT_H / 2, zone: zone + 1 });
            }
        }
        rowOffset += rowsInZone;
    }
    return out;
})();
// ── Entry / overwing door positions (source SVG coords) ──────────────────
// Same coordinate space as the cargo doors. Source HIGH y is the
// starboard edge (R doors), source LOW y is the port edge (L doors).
// L1/R1 sit forward of zone 1 cabin start; L2/R2 + L3/R3 are at the
// overwing exit stations within the wing band; L4/R4 sit aft of zone 4
// where the tail begins to taper, so they're rotated slightly to follow
// the fuselage edge. Rect size (18×10) is smaller than the cargo doors
// (41×14) so the silhouette reads correctly at a glance — pax doors are
// visibly narrower than cargo doors in real life. Coords + rotations
// match ViewWeightBalance.xaml exactly so the WPF + web silhouettes
// stay aligned.
const DOOR_ENTRY_W = 13;
const DOOR_ENTRY_H = 7;
const ENTRY_DOORS = [
    { id: "L1", x: 563, y: 348 },
    { id: "L2", x: 415, y: 348 },
    { id: "L3", x: 400, y: 348 },
    { id: "L4", x: 215, y: 352, rotate: -8.374 },
    { id: "R1", x: 563, y: 395 },
    { id: "R2", x: 415, y: 395 },
    { id: "R3", x: 400, y: 395 },
    { id: "R4", x: 215, y: 392, rotate: 8.902 },
];
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
    // Passenger simulation UI state. Mirrors the FMS-sync flash pattern —
    // pending while the POST is in flight, success/error on resolution,
    // cleared by a setTimeout. Manifest persists across the success flash
    // so the user can see the generated names after the toast clears.
    const [simOpen, setSimOpen] = useState(false);
    const [simCount, setSimCount] = useState("");
    const [simStatus, setSimStatus] = useState("idle");
    const [simMessage, setSimMessage] = useState("");
    const [manifest, setManifest] = useState(null);
    const [manifestOpen, setManifestOpen] = useState(false);
    const simTimerRef = useRef(null);
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
        if (simTimerRef.current !== null) {
            window.clearTimeout(simTimerRef.current);
            simTimerRef.current = null;
        }
    }, []);
    // Fetch the cached simulation manifest on mount so a refresh of the
    // page after a SIMULATE click brings back the same names. Server holds
    // it in-memory only — non-zero totalPassengers means a manifest exists.
    useEffect(() => {
        let cancelled = false;
        (async () => {
            try {
                const m = await get("/passengers/manifest");
                if (!cancelled && m && m.totalPassengers > 0)
                    setManifest(m);
            }
            catch {
                /* useApi already handled 401; not having a manifest is fine */
            }
        })();
        return () => { cancelled = true; };
    }, [get]);
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
    const triggerSimFlash = (status, message, durationMs) => {
        setSimStatus(status);
        setSimMessage(message);
        if (simTimerRef.current !== null)
            window.clearTimeout(simTimerRef.current);
        simTimerRef.current = window.setTimeout(() => {
            setSimStatus("idle");
            setSimMessage("");
            simTimerRef.current = null;
        }, durationMs);
    };
    const handleSimulate = async () => {
        if (simStatus === "pending")
            return;
        let parsedCount;
        const trimmed = simCount.trim();
        if (trimmed !== "") {
            const n = parseInt(trimmed, 10);
            if (Number.isNaN(n) || n < 0) {
                triggerSimFlash("error", "Invalid count", 4000);
                return;
            }
            parsedCount = n;
        }
        setSimStatus("pending");
        setSimMessage("");
        try {
            const result = await post("/passengers/simulate", { count: parsedCount });
            if (result?.success && result.manifest) {
                setManifest(result.manifest);
                setManifestOpen(true);
                triggerSimFlash("success", `Generated ${result.manifest.totalPassengers} pax`, 3000);
            }
            else {
                triggerSimFlash("error", result?.errorMessage || "Generation failed", 5000);
            }
        }
        catch (e) {
            triggerSimFlash("error", e?.message || "Generation failed", 5000);
        }
    };
    const handleClearPax = async () => {
        if (simStatus === "pending")
            return;
        setSimStatus("pending");
        setSimMessage("");
        try {
            const result = await post("/passengers/clear");
            if (result?.success) {
                setManifest(null);
                triggerSimFlash("success", "Cabin cleared", 2000);
            }
            else {
                triggerSimFlash("error", result?.errorMessage || "Clear failed", 5000);
            }
        }
        catch (e) {
            triggerSimFlash("error", e?.message || "Clear failed", 5000);
        }
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
                                            : styles.fmsSyncMessageError, children: syncMessage }))] })] })] }), _jsxs("section", { className: styles.dataCol, children: [_jsx("h2", { className: styles.colHeading, children: "Passengers" }), _jsxs("div", { className: styles.dataCard, children: [_jsxs("div", { className: styles.capacity, children: ["CAPACITY: ", totalCapacity, " ECONOMY"] }), _jsxs("div", { className: styles.dataRow, children: [_jsx("span", { className: styles.label, children: "Pax" }), _jsx("span", { className: styles.headerCell, children: "PLANNED" }), _jsx("span", { className: styles.headerCell, children: "BOARDED" })] }), _jsxs("div", { className: styles.dataRow, children: [_jsx("span", { className: styles.label, children: "\u00A0" }), _jsx("span", { className: styles.value, children: wb.passengersPlanned }), _jsx("span", { className: styles.value, children: wb.passengersBoarded })] }), _jsxs("div", { className: styles.simulateRow, children: [!simOpen ? (_jsx("button", { type: "button", className: styles.simulateButton, onClick: () => setSimOpen(true), children: "SIMULATE" })) : (_jsxs(_Fragment, { children: [_jsxs("label", { className: styles.simulateLabel, children: ["COUNT", _jsx("input", { type: "number", min: 0, max: totalCapacity, value: simCount, placeholder: String(totalCapacity), onChange: e => setSimCount(e.target.value), className: styles.simulateInput })] }), _jsx("button", { type: "button", className: [
                                                    styles.simulateButton,
                                                    styles.simulateGenerate,
                                                    simStatus === "success" ? styles.simulateSuccess : "",
                                                    simStatus === "error" ? styles.simulateError : "",
                                                ].filter(Boolean).join(" "), disabled: simStatus === "pending", onClick: handleSimulate, children: simStatus === "pending" ? "…" : "GENERATE" }), _jsx("button", { type: "button", className: styles.simulateButton, disabled: simStatus === "pending", onClick: handleClearPax, children: "CLEAR" }), _jsx("button", { type: "button", className: styles.simulateClose, onClick: () => setSimOpen(false), title: "Hide controls", children: "\u00D7" })] })), simMessage && (_jsx("span", { className: simStatus === "success" ? styles.simulateMessageOk :
                                            simStatus === "error" ? styles.simulateMessageError : "", children: simMessage }))] }), simOpen && (_jsx("p", { className: styles.simulateNote, children: "SIMULATE works for headless cabins; GSX boarding will overwrite." }))] }), _jsx("h2", { className: styles.colHeading, children: "Cargo" }), _jsxs("div", { className: styles.dataCard, children: [_jsxs("div", { className: styles.capacity, children: ["CAPACITY:\u00A0", wb.cargoFwdCapacityKg.toLocaleString(), " KG FWD,\u00A0", wb.cargoAftCapacityKg.toLocaleString(), " KG AFT,\u00A0", wb.cargoBulkCapacityKg.toLocaleString(), " KG BULK"] }), _jsxs("div", { className: styles.dataRow, children: [_jsx("span", { className: styles.label, children: "Cargo" }), _jsx("span", { className: styles.headerCell, children: "PLANNED (KG)" }), _jsx("span", { className: styles.headerCell, children: "LOADED (KG)" })] }), _jsxs("div", { className: styles.dataRow, children: [_jsx("span", { className: styles.label, children: "\u00A0" }), _jsx("span", { className: styles.value, children: wb.cargoPlannedKg.toLocaleString(undefined, { maximumFractionDigits: 0 }) }), _jsx("span", { className: styles.value, children: cargoLoadedTotal.toLocaleString(undefined, { maximumFractionDigits: 0 }) })] }), _jsxs("div", { className: styles.dataRow, children: [_jsx("span", { className: styles.label, children: "FWD / AFT" }), _jsx("span", { className: styles.value, children: "\u00A0" }), _jsxs("span", { className: styles.subValue, children: [wb.cargoFwdLoadedKg.toLocaleString(undefined, { maximumFractionDigits: 0 }), " / ", wb.cargoAftLoadedKg.toLocaleString(undefined, { maximumFractionDigits: 0 })] })] })] }), _jsx("h2", { className: styles.colHeading, children: "Aircraft Status" }), _jsxs("div", { className: styles.dataCard, children: [_jsx("svg", { viewBox: "0 270 750 210", className: styles.silhouetteSvg, children: _jsxs("g", { transform: "rotate(-180 375 375)", children: [_jsx("path", { d: A320_OUTLINE_PATH, fill: "#3F3F3F", stroke: "#7A7A7A", strokeWidth: 2 }), (() => {
                                            const occupied = parseSeatOccupation(wb.seatOccupation);
                                            return SEAT_RECTS.map((s, i) => (_jsx("rect", { x: s.x, y: s.y, width: SEAT_RECT_W, height: SEAT_RECT_H, fill: occupied[i] ? SEAT_OCCUPIED_COLOR : SEAT_EMPTY_COLOR, stroke: "#1A1A1A", strokeWidth: 0.4 }, `seat-${i}`)));
                                        })(), _jsx("rect", { x: 498, y: 403, width: 41, height: 14, fill: doorColor(wb.fwdCargoDoorOpen), stroke: "#FFFFFF", strokeWidth: 1.2 }), _jsx("rect", { x: 293, y: 403, width: 41, height: 14, fill: doorColor(wb.aftCargoDoorOpen), stroke: "#FFFFFF", strokeWidth: 1.2 }), _jsx("rect", { x: 255, y: 403, width: 20, height: 11, transform: "rotate(-0.265 265 408.5)", fill: doorColor(wb.bulkCargoDoorOpen, wb.cargoBulkCapacityKg > 0), stroke: "#FFFFFF", strokeWidth: 1.2 }), ENTRY_DOORS.map(d => {
                                            const open = d.id === "L1" ? wb.door1LOpen :
                                                d.id === "R1" ? wb.door1ROpen :
                                                    d.id === "L2" ? wb.door2LOpen :
                                                        d.id === "R2" ? wb.door2ROpen :
                                                            d.id === "L3" ? wb.door3LOpen :
                                                                d.id === "R3" ? wb.door3ROpen :
                                                                    d.id === "L4" ? wb.door4LOpen :
                                                                        wb.door4ROpen;
                                            const cx = d.x + DOOR_ENTRY_W / 2;
                                            const cy = d.y + DOOR_ENTRY_H / 2;
                                            const transform = d.rotate !== undefined
                                                ? `rotate(${d.rotate} ${cx} ${cy})`
                                                : undefined;
                                            return (_jsx("rect", { x: d.x, y: d.y, width: DOOR_ENTRY_W, height: DOOR_ENTRY_H, transform: transform, fill: doorColor(open), stroke: "#FFFFFF", strokeWidth: 1 }, `entry-${d.id}`));
                                        })] }) }), _jsxs("div", { className: styles.doorStatusGrid, children: [_jsxs("div", { className: styles.doorStatusCell, children: [_jsx("span", { className: styles.doorStatusLabel, children: "FWD" }), _jsx("span", { className: styles.doorStatusValue, style: { color: doorColor(wb.fwdCargoDoorOpen) }, children: doorStatus(wb.fwdCargoDoorOpen) })] }), _jsxs("div", { className: styles.doorStatusCell, children: [_jsx("span", { className: styles.doorStatusLabel, children: "AFT" }), _jsx("span", { className: styles.doorStatusValue, style: { color: doorColor(wb.aftCargoDoorOpen) }, children: doorStatus(wb.aftCargoDoorOpen) })] }), _jsxs("div", { className: styles.doorStatusCell, children: [_jsx("span", { className: styles.doorStatusLabel, children: "BULK" }), _jsx("span", { className: styles.doorStatusValue, style: { color: doorColor(wb.bulkCargoDoorOpen, wb.cargoBulkCapacityKg > 0) }, children: doorStatus(wb.bulkCargoDoorOpen, wb.cargoBulkCapacityKg > 0) })] })] }), _jsx("div", { className: [
                                    styles.readinessBanner,
                                    wb.allDoorsClosed ? styles.readinessOk : styles.readinessOpen,
                                ].join(" "), children: wb.allDoorsClosed ? "ALL DOORS CLOSED" : "DOORS OPEN" }), manifest && manifest.totalPassengers > 0 && (_jsxs("div", { className: styles.manifestSection, children: [_jsxs("button", { type: "button", className: styles.manifestToggle, onClick: () => setManifestOpen(o => !o), "aria-expanded": manifestOpen, children: [_jsxs("span", { children: [manifestOpen ? "▾" : "▸", " MANIFEST (", manifest.totalPassengers, ")"] }), !manifest.seatOccupationWritten && (_jsx("span", { className: styles.manifestWarn, title: "seatOccupation write failed", children: "\u26A0" }))] }), manifestOpen && (_jsx("div", { className: styles.manifestTableWrap, children: _jsxs("table", { className: styles.manifestTable, children: [_jsx("thead", { children: _jsxs("tr", { children: [_jsx("th", { children: "SEAT" }), _jsx("th", { children: "NAME" }), _jsx("th", { children: "ZONE" })] }) }), _jsx("tbody", { children: manifest.passengers.map(p => (_jsxs("tr", { children: [_jsx("td", { children: p.seatNumber }), _jsxs("td", { children: [p.firstName, " ", p.lastName] }), _jsxs("td", { children: ["Z", p.zone] })] }, p.seatNumber))) })] }) }))] }))] }), _jsx("h2", { className: styles.colHeading, children: "Fuel" }), _jsxs("div", { className: styles.dataCard, children: [_jsxs("div", { className: styles.capacity, children: ["CAPACITY USABLE ", wb.fuelCapacityKg.toLocaleString(undefined, { maximumFractionDigits: 0 }), " KG \u2014 SG: 0.80"] }), _jsxs("div", { className: styles.dataRow, children: [_jsx("span", { className: styles.label, children: "Fuel" }), _jsx("span", { className: styles.headerCell, children: "PLANNED (KG)" }), _jsx("span", { className: styles.headerCell, children: "IN TANKS (KG)" })] }), _jsxs("div", { className: styles.dataRow, children: [_jsx("span", { className: styles.label, children: "\u00A0" }), _jsx("span", { className: styles.value, children: wb.fuelPlannedKg.toLocaleString(undefined, { maximumFractionDigits: 0 }) }), _jsx("span", { className: styles.value, children: wb.fuelInTanksKg.toLocaleString(undefined, { maximumFractionDigits: 0 }) })] })] })] })] }));
}
