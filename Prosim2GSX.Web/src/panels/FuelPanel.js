import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useEffect } from "react";
import { useApi } from "../api/useApi";
import { useAppState } from "../state/AppStateContext";
import styles from "./FuelPanel.module.css";
// Read-only Fuel panel. Initial REST load on mount; live updates arrive
// through the WebSocket "fuel" channel and are merged into AppState by the
// default reducer branch.
//
// All thresholds (under-fuelled, capacity-bar amber/red bands, delta
// colour bands) are surfaced on the wire via the DTO so the panel doesn't
// duplicate magic numbers — server is the single source of truth.
// Capacity-bar colour bands. Wider tolerance than the delta indicator
// because the bar shows physical fuel quantity (100 kg over plan is fine
// to look "green"); the delta indicator is the operator-facing alert.
const CAPACITY_OVER_AMBER_KG = 200;
const CAPACITY_UNDER_RED_KG = 100;
// Delta-indicator colour bands. Tighter — any overage above the spec's
// 100 kg threshold flips the indicator amber so the operator catches it.
const DELTA_AMBER_KG = 100;
const DELTA_RED_KG = 100;
export function FuelPanel() {
    const { get } = useApi();
    const { state, dispatch } = useAppState();
    useEffect(() => {
        let cancelled = false;
        (async () => {
            try {
                const dto = await get("/fuel");
                if (!cancelled) {
                    dispatch({
                        type: "set",
                        channel: "fuel",
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
    const f = state.fuel;
    if (!f) {
        return _jsx("div", { className: styles.loading, children: "Loading fuel\u2026" });
    }
    // Capacity-bar fill ratio (0 – 1). Clamped — if the dataref ever
    // overshoots capacity (refuel target above usable, simulator quirk),
    // the bar caps at 100% rather than blowing the layout.
    const capacityRatio = f.fuelCapacityKg > 0
        ? Math.min(1, Math.max(0, f.fuelInTanksKg / f.fuelCapacityKg))
        : 0;
    // Capacity-bar colour. Only meaningful when a plan exists; with no plan
    // the bar stays neutral (grey/blue) so the panel doesn't mislead by
    // showing red/amber for a deliberately empty aircraft.
    const capacityClass = f.plannedRampKg > 0
        ? (f.fuelDeltaKg > CAPACITY_OVER_AMBER_KG
            ? styles.capacityFillAmber
            : f.fuelDeltaKg < -CAPACITY_UNDER_RED_KG
                ? styles.capacityFillRed
                : styles.capacityFillGreen)
        : styles.capacityFillNeutral;
    // Delta-row colour. Same suppression rule — no plan means no delta to
    // colour-code.
    const deltaClass = f.plannedRampKg > 0
        ? (f.fuelDeltaKg > DELTA_AMBER_KG
            ? styles.deltaAmber
            : f.fuelDeltaKg < -DELTA_RED_KG
                ? styles.deltaRed
                : styles.deltaGreen)
        : styles.deltaNeutral;
    const tankRatio = (kg, cap) => cap > 0 ? Math.min(1, Math.max(0, kg / cap)) : 0;
    const fmtKg = (n) => n.toLocaleString(undefined, { maximumFractionDigits: 0 });
    return (_jsxs("div", { className: styles.panel, children: [_jsx("h2", { className: styles.colHeading, children: "Fuel Summary" }), _jsxs("div", { className: styles.dataCard, children: [_jsxs("div", { className: styles.capacity, children: ["CAPACITY USABLE ", fmtKg(f.fuelCapacityKg), " KG \u2014 SG: ", f.specificGravity.toFixed(2)] }), _jsxs("div", { className: styles.capacityBar, children: [_jsx("div", { className: `${styles.capacityFill} ${capacityClass}`, style: { width: `${capacityRatio * 100}%` } }), _jsxs("span", { className: styles.capacityLabel, children: [fmtKg(f.fuelInTanksKg), " / ", fmtKg(f.fuelCapacityKg), " KG"] })] }), _jsxs("div", { className: styles.summaryGrid, children: [_jsx("div", { className: styles.summaryHeader, children: "PLANNED (KG)" }), _jsx("div", { className: styles.summaryHeader, children: "IN TANKS (KG)" }), _jsx("div", { className: styles.summaryValue, children: fmtKg(f.plannedRampKg) }), _jsx("div", { className: styles.summaryValue, children: fmtKg(f.fuelInTanksKg) }), _jsx("div", { className: styles.summaryHeader, children: "PLANNED (L)" }), _jsx("div", { className: styles.summaryHeader, children: "IN TANKS (L)" }), _jsx("div", { className: styles.summaryValue, children: fmtKg(f.plannedRampLitres) }), _jsx("div", { className: styles.summaryValue, children: fmtKg(f.fuelInTanksLitres) })] }), _jsxs("div", { className: `${styles.deltaRow} ${deltaClass}`, children: [_jsx("span", { className: styles.deltaLabel, children: "DELTA:" }), _jsx("span", { className: styles.deltaValue, children: f.plannedRampKg > 0
                                    ? `${f.fuelDeltaKg >= 0 ? "+" : ""}${fmtKg(f.fuelDeltaKg)} KG`
                                    : "— (no flight plan)" }), f.plannedRampKg > 0 && (_jsx("span", { className: styles.deltaTag, children: f.isOverFuelled ? "OVER" : f.isUnderFuelled ? "UNDER" : "OK" }))] })] }), _jsx("h2", { className: styles.colHeading, children: "Tank Breakdown" }), _jsxs("div", { className: styles.dataCard, children: [_jsx(TankRow, { label: "CENTRE", kg: f.fuelCentreKg, capacityKg: f.fuelCentreCapacityKg, ratio: tankRatio(f.fuelCentreKg, f.fuelCentreCapacityKg), fmtKg: fmtKg }), _jsx(TankRow, { label: "LEFT", kg: f.fuelLeftKg, capacityKg: f.fuelLeftCapacityKg, ratio: tankRatio(f.fuelLeftKg, f.fuelLeftCapacityKg), fmtKg: fmtKg }), _jsx(TankRow, { label: "RIGHT", kg: f.fuelRightKg, capacityKg: f.fuelRightCapacityKg, ratio: tankRatio(f.fuelRightKg, f.fuelRightCapacityKg), fmtKg: fmtKg })] }), _jsx("p", { className: styles.note, children: "NOTE: THE ABOVE FIGURES ARE LIVE." })] }));
}
function TankRow({ label, kg, capacityKg, ratio, fmtKg }) {
    return (_jsxs("div", { className: styles.tankRow, children: [_jsx("div", { className: styles.tankLabel, children: label }), _jsx("div", { className: styles.tankBar, children: _jsx("div", { className: styles.tankFill, style: { width: `${ratio * 100}%` } }) }), _jsxs("div", { className: styles.tankValue, children: [fmtKg(kg), " / ", fmtKg(capacityKg), " KG"] })] }));
}
