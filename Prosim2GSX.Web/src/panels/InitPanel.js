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
// Per-row inline override editing lands in the next polish slice. v1
// shows the effective values (override-tinted) and the four macro
// buttons: FETCH OFP, SYNC TO FMS, CLEAR OVERRIDES, RESET FLIGHT.
export function InitPanel() {
    const { get, post } = useApi();
    const { state, dispatch } = useAppState();
    const [busy, setBusy] = useState(null);
    const [resetArmed, setResetArmed] = useState(false);
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
    const hasOverrides = Object.values(efb.overrideFlags ?? {}).some((v) => v);
    return (_jsx("div", { className: styles.panel, children: _jsxs("div", { className: styles.inner, children: [_jsxs("div", { className: styles.columns, children: [_jsxs("div", { className: styles.column, children: [_jsx("div", { className: styles.title, children: "ACTIVE / INIT" }), _jsx("div", { className: styles.separator }), _jsx(Row, { label: "FLT NBR", value: ofp?.flightNumber }), _jsxs("div", { className: styles.rowSplit, children: [_jsx("span", { className: styles.label, children: "FROM" }), _jsx(Value, { text: ofp?.departureIcao }), _jsx("span", { className: styles.label, children: "TO" }), _jsx(Value, { text: ofp?.arrivalIcao }), _jsx("span", { className: styles.label, children: "ALTN" }), _jsx(Value, { text: ofp?.alternateIcao })] }), _jsx(Row, { label: "RWY OUT/IN", value: formatRwys(ofp) }), _jsx(Row, { label: "CALLSIGN", value: ofp?.callsign }), _jsx("div", { className: styles.separator }), _jsx(Row, { label: "CRZ FL", value: formatFl(ofp?.cruiseFlightLevel) }), _jsx(Row, { label: "CI", value: formatCi(ofp?.costIndex) }), _jsx(Row, { label: "CPNY RTE", value: ofp?.route }), _jsx("div", { className: styles.separator }), _jsx(Row, { label: "STD", value: formatZulu(ofp?.std) }), _jsx(Row, { label: "ETA", value: formatZulu(ofp?.eta) }), _jsx("div", { className: styles.separator }), _jsx(StatusLine, { efb: efb }), efb.lastFetchError && _jsx("div", { className: styles.errorLine, children: efb.lastFetchError })] }), _jsxs("div", { className: styles.column, children: [_jsx("div", { className: styles.title, children: "DATA / STATUS" }), _jsx("div", { className: styles.separator }), _jsx(Row, { label: "ACFT", value: ofp?.aircraftType }), _jsx(Row, { label: "REG", value: ofp?.aircraftReg }), _jsx("div", { className: styles.separator }), _jsx(FuelRow, { label: "ZFW", value: formatKg(effective(efb, "zfwKg", ofp?.zfwKg)), overridden: isOverridden(efb, "zfwKg") }), _jsx(FuelRow, { label: "OEW", value: formatKg(ofp?.oewKg) }), _jsx(FuelRow, { label: "FUEL RAMP", value: formatKg(effective(efb, "fuelRampKg", ofp?.fuelRampKg)), overridden: isOverridden(efb, "fuelRampKg") }), _jsx(FuelRow, { label: "FUEL TRIP", value: formatKg(ofp?.fuelTripKg) }), _jsx(FuelRow, { label: "FUEL MIN", value: formatKg(ofp?.fuelMinimumKg) }), _jsx(FuelRow, { label: "FUEL EXTRA", value: formatKg(ofp?.fuelExtraKg) }), _jsx(FuelRow, { label: "PAX", value: formatPax(effective(efb, "passengerCount", ofp?.passengerCount)), overridden: isOverridden(efb, "passengerCount") }), _jsx(FuelRow, { label: "CARGO", value: formatKg(effective(efb, "cargoKg", ofp?.cargoKg)), overridden: isOverridden(efb, "cargoKg") }), _jsx("div", { className: styles.separator }), _jsx("div", { className: styles.fetchedAt, children: formatFetchedAt(efb.fetchedAt) })] })] }), _jsxs("div", { className: styles.actions, children: [_jsx("button", { type: "button", className: `${styles.btn} ${styles.btnPrimary}`, onClick: onFetch, disabled: busy !== null || efb.isBusy, children: busy === "fetch" || efb.isBusy ? "FETCHING…" : "FETCH OFP" }), _jsx("button", { type: "button", className: `${styles.btn} ${styles.btnPrimary}`, onClick: onSync, disabled: busy !== null || !efb.isOfpLoaded, children: busy === "sync" ? "SYNCING…" : "SYNC TO FMS" }), _jsx("button", { type: "button", className: `${styles.btn} ${styles.btnAmber}`, onClick: onClearOverrides, disabled: busy !== null || !hasOverrides, children: busy === "clear" ? "CLEARING…" : "CLEAR OVERRIDES" }), _jsx("button", { type: "button", className: `${styles.btn} ${styles.btnWarn}`, onClick: onReset, disabled: busy !== null, children: busy === "reset" ? "RESETTING…" : resetArmed ? "CONFIRM RESET" : "RESET FLIGHT" })] })] }) }));
}
// ── Sub-components ─────────────────────────────────────────────────────────
function Row({ label, value }) {
    return (_jsxs("div", { className: styles.row, children: [_jsx("span", { className: styles.label, children: label }), _jsx(Value, { text: value })] }));
}
function FuelRow({ label, value, overridden }) {
    const valueClass = `${styles.valueRight} ${overridden ? styles.valueOverridden : ""}`;
    return (_jsxs("div", { className: styles.rowFuel, children: [_jsx("span", { className: styles.label, children: label }), _jsx("span", { className: valueClass, children: value })] }));
}
function Value({ text }) {
    const t = (text ?? "").trim();
    if (!t)
        return _jsx("span", { className: styles.value, children: "\u2014" });
    return _jsx("span", { className: styles.value, children: t });
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
