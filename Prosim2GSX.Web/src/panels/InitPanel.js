import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useEffect, useState } from "react";
import { useApi } from "../api/useApi";
import { useAppState } from "../state/AppStateContext";
import styles from "./InitPanel.module.css";
// EFB Flight Planning (INIT) tab — Airbus FMS-style layout. Reads from
// state.efbFlightPlan (initial REST fetch on mount, live WS snapshots on
// the "efbFlightPlan" channel). Action buttons hit the matching REST
// endpoints; the server broadcasts and AppState updates automatically.
//
// The four writable rows (ZFW / FUEL RAMP / PAX / CARGO) are click-to-
// edit when LockFieldsFromOfp is true: the value cell turns into an
// inline input with ENT / CLR keys. Range-validated against the bounds
// in FIELD_RANGES below; out-of-range commits flash a red "OUT OF
// RANGE" hint and reject without writing.
// Sensible per-field bounds for override validation. Loose enough to
// allow non-A320 airframes, tight enough to catch typos like an extra
// digit.
const FIELD_RANGES = {
    zfwKg: { min: 30000, max: 70000 },
    fuelRampKg: { min: 0, max: 25000 },
    passengerCount: { min: 0, max: 220, integer: true },
    cargoKg: { min: 0, max: 8000 },
};
export function InitPanel() {
    const { get, post } = useApi();
    const { state, dispatch } = useAppState();
    const [busy, setBusy] = useState(null);
    const [resetArmed, setResetArmed] = useState(false);
    // Inline-editor state. Only one row is ever in edit mode at a time;
    // committing or cancelling clears all three.
    const [editingField, setEditingField] = useState(null);
    const [editValue, setEditValue] = useState("");
    const [editError, setEditError] = useState(null);
    // Fake-phased fetch indicator — the server-side fetch is monolithic
    // (one HTTP call, one broadcast), but the prompt's spec calls for the
    // text to cycle through phases so the user sees something happening.
    // Cleared when isBusy goes false.
    const [fetchPhase, setFetchPhase] = useState("");
    useEffect(() => {
        let cancelled = false;
        (async () => {
            try {
                const dto = await get("/efb/flight-plan");
                if (!cancelled) {
                    dispatch({
                        type: "set",
                        channel: "efbFlightPlan",
                        state: dto,
                    });
                }
            }
            catch {
                /* WS will fill in once connected; useApi already handled 401 */
            }
        })();
        return () => { cancelled = true; };
    }, [get, dispatch]);
    const efb = state.efbFlightPlan;
    // Cycle the fetch-phase text while a fetch is in flight. Order matches
    // the actual server-side sequence (HTTP fetch → JSON parse → ProSim
    // dataref writes); each step displays for ~700ms.
    useEffect(() => {
        if (!efb?.isBusy) {
            setFetchPhase("");
            return;
        }
        const phases = [
            "CONTACTING SIMBRIEF...",
            "PARSING OFP...",
            "WRITING TO FMS...",
        ];
        let i = 0;
        setFetchPhase(phases[0]);
        const interval = window.setInterval(() => {
            i = (i + 1) % phases.length;
            setFetchPhase(phases[i]);
        }, 700);
        return () => window.clearInterval(interval);
    }, [efb?.isBusy]);
    if (!efb) {
        return _jsx("div", { className: styles.loading, children: "LOADING INIT\u2026" });
    }
    const ofp = efb.ofp;
    const onFetch = async () => {
        setBusy("fetch");
        try {
            const req = {
                departure: ofp?.departureIcao ?? "",
                arrival: ofp?.arrivalIcao ?? "",
                alternate: ofp?.alternateIcao ?? "",
                flightNumber: ofp?.flightNumber ?? "",
            };
            await post("/efb/fetch-ofp", req);
        }
        catch {
            /* lastFetchError surfaced in the broadcast */
        }
        finally {
            setBusy(null);
        }
    };
    const onSync = async () => {
        setBusy("sync");
        try {
            await post("/efb/sync-to-fms");
        }
        catch { /* server logs */ }
        finally {
            setBusy(null);
        }
    };
    const onClearOverrides = async () => {
        setBusy("clear");
        try {
            await post("/efb/clear-all-overrides");
        }
        catch { /* server logs */ }
        finally {
            setBusy(null);
        }
    };
    const onReset = async () => {
        if (!resetArmed) {
            setResetArmed(true);
            window.setTimeout(() => setResetArmed(false), 3000);
            return;
        }
        setResetArmed(false);
        setBusy("reset");
        try {
            await post("/efb/reset-flight");
        }
        catch { /* server logs */ }
        finally {
            setBusy(null);
        }
    };
    const startEdit = (field, currentValue) => {
        if (!efb.isOfpLoaded)
            return;
        setEditingField(field);
        setEditValue(currentValue > 0 ? Math.round(currentValue).toString() : "");
        setEditError(null);
    };
    const cancelEdit = () => {
        setEditingField(null);
        setEditValue("");
        setEditError(null);
    };
    const commitEdit = async () => {
        const field = editingField;
        if (!field)
            return;
        const range = FIELD_RANGES[field];
        if (!range) {
            cancelEdit();
            return;
        }
        const num = parseFloat(editValue.trim());
        if (!Number.isFinite(num)) {
            flashError("INVALID");
            return;
        }
        if (num < range.min || num > range.max) {
            flashError("OUT OF RANGE");
            return;
        }
        const value = range.integer ? Math.round(num) : num;
        try {
            await post("/efb/override", { field, value });
            cancelEdit();
        }
        catch {
            flashError("WRITE FAILED");
        }
    };
    const flashError = (msg) => {
        setEditError(msg);
        window.setTimeout(() => setEditError((prev) => (prev === msg ? null : prev)), 2000);
    };
    const hasOverrides = Object.values(efb.overrideFlags ?? {}).some((v) => v);
    const editable = efb.lockFieldsFromOfp;
    // Pending mode applies when no OFP is loaded — every OFP-derived field
    // renders as an amber-filled block with a 1Hz blinking cursor instead
    // of a green "—". An OFP-loaded panel with one missing field still
    // shows "—" green (data is there, field just isn't populated).
    const pending = !efb.isOfpLoaded;
    return (_jsx("div", { className: styles.panel, children: _jsxs("div", { className: styles.inner, children: [_jsxs("div", { className: styles.columns, children: [_jsxs("div", { className: styles.column, children: [_jsx("div", { className: styles.title, children: "ACTIVE / INIT" }), _jsx("div", { className: styles.separator }), _jsx(Row, { label: "FLT NBR", value: ofp?.flightNumber, pending: pending, pendingLen: 6 }), _jsxs("div", { className: styles.rowSplit, children: [_jsx("span", { className: styles.label, children: "FROM" }), _jsx(Value, { text: ofp?.departureIcao, pending: pending, pendingLen: 4 }), _jsx("span", { className: styles.label, children: "TO" }), _jsx(Value, { text: ofp?.arrivalIcao, pending: pending, pendingLen: 4 }), _jsx("span", { className: styles.label, children: "ALTN" }), _jsx(Value, { text: ofp?.alternateIcao, pending: pending, pendingLen: 4 })] }), _jsx(Row, { label: "RWY OUT/IN", value: formatRwys(ofp), pending: pending, pendingLen: 5 }), _jsx(Row, { label: "CALLSIGN", value: ofp?.callsign, pending: pending, pendingLen: 6 }), _jsx("div", { className: styles.separator }), _jsx(Row, { label: "CRZ FL", value: formatFl(ofp?.cruiseFlightLevel), pending: pending, pendingLen: 5 }), _jsx(Row, { label: "CI", value: formatCi(ofp?.costIndex), pending: pending, pendingLen: 3 }), _jsx(Row, { label: "CPNY RTE", value: ofp?.route, pending: pending, pendingLen: 10 }), _jsx("div", { className: styles.separator }), _jsx(Row, { label: "STD", value: formatZulu(ofp?.std), pending: pending, pendingLen: 4 }), _jsx(Row, { label: "ETA", value: formatZulu(ofp?.eta), pending: pending, pendingLen: 4 }), _jsx("div", { className: styles.separator }), _jsx(StatusLine, { efb: efb }), efb.lastFetchError && _jsx("div", { className: styles.errorLine, children: efb.lastFetchError }), fetchPhase && _jsx("div", { className: styles.fetchPhase, children: fetchPhase })] }), _jsxs("div", { className: styles.column, children: [_jsx("div", { className: styles.title, children: "DATA / STATUS" }), _jsx("div", { className: styles.separator }), _jsx(Row, { label: "ACFT", value: ofp?.aircraftType, pending: pending, pendingLen: 8 }), _jsx(Row, { label: "REG", value: ofp?.aircraftReg, pending: pending, pendingLen: 6 }), _jsx("div", { className: styles.separator }), _jsx(EditableFuelRow, { label: "ZFW", field: "zfwKg", value: formatKg(effective(efb, "zfwKg", ofp?.zfwKg)), rawValue: effective(efb, "zfwKg", ofp?.zfwKg), overridden: isOverridden(efb, "zfwKg"), editable: editable && efb.isOfpLoaded, pending: pending, editingField: editingField, editValue: editValue, editError: editError, onStart: startEdit, onChange: setEditValue, onCommit: commitEdit, onCancel: cancelEdit }), _jsx(FuelRow, { label: "OEW", value: formatKg(ofp?.oewKg), pending: pending }), _jsx(EditableFuelRow, { label: "FUEL RAMP", field: "fuelRampKg", value: formatKg(effective(efb, "fuelRampKg", ofp?.fuelRampKg)), rawValue: effective(efb, "fuelRampKg", ofp?.fuelRampKg), overridden: isOverridden(efb, "fuelRampKg"), editable: editable && efb.isOfpLoaded, pending: pending, editingField: editingField, editValue: editValue, editError: editError, onStart: startEdit, onChange: setEditValue, onCommit: commitEdit, onCancel: cancelEdit }), _jsx(FuelRow, { label: "FUEL TRIP", value: formatKg(ofp?.fuelTripKg), pending: pending }), _jsx(FuelRow, { label: "FUEL MIN", value: formatKg(ofp?.fuelMinimumKg), pending: pending }), _jsx(FuelRow, { label: "FUEL EXTRA", value: formatKg(ofp?.fuelExtraKg), pending: pending }), _jsx(EditableFuelRow, { label: "PAX", field: "passengerCount", value: formatPax(effective(efb, "passengerCount", ofp?.passengerCount)), rawValue: effective(efb, "passengerCount", ofp?.passengerCount), overridden: isOverridden(efb, "passengerCount"), editable: editable && efb.isOfpLoaded, pending: pending, editingField: editingField, editValue: editValue, editError: editError, onStart: startEdit, onChange: setEditValue, onCommit: commitEdit, onCancel: cancelEdit }), _jsx(EditableFuelRow, { label: "CARGO", field: "cargoKg", value: formatKg(effective(efb, "cargoKg", ofp?.cargoKg)), rawValue: effective(efb, "cargoKg", ofp?.cargoKg), overridden: isOverridden(efb, "cargoKg"), editable: editable && efb.isOfpLoaded, pending: pending, editingField: editingField, editValue: editValue, editError: editError, onStart: startEdit, onChange: setEditValue, onCommit: commitEdit, onCancel: cancelEdit }), _jsx("div", { className: styles.separator }), _jsx("div", { className: styles.fetchedAt, children: formatFetchedAt(efb.fetchedAt) })] })] }), _jsxs("div", { className: styles.actions, children: [_jsx("button", { type: "button", className: `${styles.btn} ${styles.btnPrimary}`, onClick: onFetch, disabled: busy !== null || efb.isBusy, children: busy === "fetch" || efb.isBusy ? "FETCHING…" : "FETCH OFP" }), _jsx("button", { type: "button", className: `${styles.btn} ${styles.btnPrimary}`, onClick: onSync, disabled: busy !== null || !efb.isOfpLoaded, children: busy === "sync" ? "SYNCING…" : "SYNC TO FMS" }), _jsx("button", { type: "button", className: `${styles.btn} ${styles.btnAmber}`, onClick: onClearOverrides, disabled: busy !== null || !hasOverrides, children: busy === "clear" ? "CLEARING…" : "CLEAR OVERRIDES" }), _jsx("button", { type: "button", className: `${styles.btn} ${styles.btnWarn}`, onClick: onReset, disabled: busy !== null, children: busy === "reset" ? "RESETTING…" : resetArmed ? "CONFIRM RESET" : "RESET FLIGHT" })] })] }) }));
}
// ── Sub-components ─────────────────────────────────────────────────────────
function Row({ label, value, pending, pendingLen }) {
    return (_jsxs("div", { className: styles.row, children: [_jsx("span", { className: styles.label, children: label }), _jsx(Value, { text: value, pending: pending, pendingLen: pendingLen })] }));
}
function FuelRow({ label, value, overridden, pending }) {
    if (pending) {
        return (_jsxs("div", { className: styles.rowFuel, children: [_jsx("span", { className: styles.label, children: label }), _jsx(PendingBoxes, { count: 5, fuelRow: true })] }));
    }
    const valueClass = `${styles.valueRight} ${overridden ? styles.valueOverridden : ""}`;
    return (_jsxs("div", { className: styles.rowFuel, children: [_jsx("span", { className: styles.label, children: label }), _jsx("span", { className: valueClass, children: value })] }));
}
// Click-to-edit row for the four writable fields. Display mode shows the
// effective value with an optional MOD chip; click flips to edit mode
// with an inline input + ENT / CLR keys. The error message ("OUT OF
// RANGE", "INVALID", "WRITE FAILED") sits next to the keys for ~2s when
// a commit is rejected.
function EditableFuelRow(props) {
    const isEditing = props.editingField === props.field;
    const error = isEditing ? props.editError : null;
    if (props.pending) {
        return (_jsxs("div", { className: styles.rowFuel, children: [_jsx("span", { className: styles.label, children: props.label }), _jsx(PendingBoxes, { count: 5, fuelRow: true })] }));
    }
    if (isEditing) {
        return (_jsxs("div", { className: styles.rowFuel, children: [_jsx("span", { className: styles.label, children: props.label }), _jsxs("div", { className: styles.editor, children: [_jsx("input", { className: styles.editorInput, type: "number", inputMode: "numeric", value: props.editValue, onChange: (e) => props.onChange(e.target.value), onKeyDown: (e) => {
                                if (e.key === "Enter") {
                                    e.preventDefault();
                                    props.onCommit();
                                }
                                else if (e.key === "Escape") {
                                    e.preventDefault();
                                    props.onCancel();
                                }
                            }, autoFocus: true }), _jsx("button", { type: "button", className: `${styles.editorKey} ${styles.editorKeyEnter}`, onClick: props.onCommit, title: "Commit override (Enter)", children: "ENT" }), _jsx("button", { type: "button", className: `${styles.editorKey} ${styles.editorKeyClr}`, onClick: props.onCancel, title: "Cancel (Esc)", children: "CLR" }), error && _jsx("span", { className: styles.editorError, children: error })] })] }));
    }
    const valueClass = `${styles.valueRight} ${props.overridden ? styles.valueOverridden : ""}`;
    const rowClass = `${styles.rowFuel} ${props.editable ? styles.rowFuelEditable : ""}`;
    return (_jsxs("div", { className: rowClass, onClick: props.editable ? () => props.onStart(props.field, props.rawValue) : undefined, title: props.editable ? "Click to override" : undefined, children: [_jsx("span", { className: styles.label, children: props.label }), _jsx("span", { className: valueClass, children: props.value }), props.overridden && _jsx("span", { className: styles.modChip, children: "MOD" })] }));
}
function Value({ text, pending, pendingLen }) {
    if (pending) {
        return _jsx(PendingBoxes, { count: pendingLen ?? 4 });
    }
    const t = (text ?? "").trim();
    if (!t)
        return _jsx("span", { className: styles.value, children: "\u2014" });
    return _jsx("span", { className: styles.value, children: t });
}
// Row of amber-outlined empty boxes — one per character slot. Used as
// the "awaiting entry" placeholder on every OFP-derived field while no
// OFP is loaded. Static (no blink) — matches the real FMS INIT page.
function PendingBoxes({ count, fuelRow }) {
    const n = Math.max(1, count);
    const cls = fuelRow ? `${styles.pending} ${styles.pendingFuel}` : styles.pending;
    return (_jsx("span", { className: cls, children: Array.from({ length: n }).map((_, i) => (_jsx("span", { className: styles.pendingBox }, i))) }));
}
function StatusLine({ efb }) {
    const { dot, label } = statusFor(efb);
    return (_jsxs("div", { className: styles.statusRow, children: [_jsx("span", { className: `${styles.statusDot} ${dot}` }), _jsx("span", { className: styles.value, children: label }), _jsx("span", { className: styles.label, children: sourceLabel(efb.source) })] }));
}
// ── Helpers ────────────────────────────────────────────────────────────────
function statusFor(efb) {
    if (efb.isBusy)
        return { dot: styles.statusDotCyan, label: "FETCHING…" };
    if (efb.lastFetchError)
        return { dot: styles.statusDotWarn, label: "UPLINK FAILED" };
    if (efb.status === "Loaded")
        return { dot: styles.statusDotGreen, label: "OFP LOADED" };
    return { dot: styles.statusDotAmber, label: "AWAITING OFP" };
}
function sourceLabel(source) {
    switch (source) {
        case "SimbriefEfb": return "SIMBRIEF";
        case "Mcdu": return "MCDU";
        case "Manual": return "MANUAL";
        default: return "—";
    }
}
function isOverridden(efb, field) {
    return !!efb.overrideFlags?.[field];
}
function effective(efb, field, ofpFallback) {
    if (!isOverridden(efb, field))
        return ofpFallback ?? 0;
    const v = efb.overrideValues?.[field];
    if (typeof v === "number")
        return v;
    if (typeof v === "string") {
        const n = parseFloat(v);
        return Number.isFinite(n) ? n : (ofpFallback ?? 0);
    }
    return ofpFallback ?? 0;
}
function formatKg(kg) {
    if (!kg || kg <= 0)
        return "—";
    return `${Math.round(kg).toLocaleString("en-US")} KG`;
}
function formatPax(n) {
    return n > 0 ? Math.round(n).toString() : "—";
}
function formatFl(fl) {
    return fl && fl > 0 ? `FL${fl.toString().padStart(3, "0")}` : "FL---";
}
function formatCi(ci) {
    return ci && ci > 0 ? ci.toString() : "—";
}
function formatZulu(iso) {
    if (!iso)
        return "—";
    const d = new Date(iso);
    if (isNaN(d.getTime()))
        return "—";
    const hh = String(d.getUTCHours()).padStart(2, "0");
    const mm = String(d.getUTCMinutes()).padStart(2, "0");
    return `${hh}${mm}Z`;
}
function formatRwys(ofp) {
    const dep = (ofp?.departurePlanRwy ?? "").trim();
    const arr = (ofp?.arrivalPlanRwy ?? "").trim();
    if (!dep && !arr)
        return "—";
    return `${dep || "—"} / ${arr || "—"}`;
}
function formatFetchedAt(iso) {
    if (!iso)
        return "";
    const d = new Date(iso);
    if (isNaN(d.getTime()))
        return "";
    const hh = String(d.getHours()).padStart(2, "0");
    const mm = String(d.getMinutes()).padStart(2, "0");
    const ss = String(d.getSeconds()).padStart(2, "0");
    return `FETCHED ${hh}:${mm}:${ss}`;
}
