import { useEffect } from "react";
import { useApi } from "../api/useApi";
import { useAppState } from "../state/AppStateContext";
import { FuelDto } from "../types";
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
const CAPACITY_UNDER_RED_KG  = 100;

// Delta-indicator colour bands. Tighter — any overage above the spec's
// 100 kg threshold flips the indicator amber so the operator catches it.
const DELTA_AMBER_KG = 100;
const DELTA_RED_KG   = 100;

export function FuelPanel() {
  const { get } = useApi();
  const { state, dispatch } = useAppState();

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const dto = await get<FuelDto>("/fuel");
        if (!cancelled) {
          dispatch({
            type: "set",
            channel: "fuel",
            state: dto as unknown as Record<string, unknown>,
          });
        }
      } catch {
        /* useApi already handled 401; WS will fill in once connected */
      }
    })();
    return () => { cancelled = true; };
  }, [get, dispatch]);

  const f = state.fuel as unknown as FuelDto | null;
  if (!f) {
    return <div className={styles.loading}>Loading fuel…</div>;
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

  const tankRatio = (kg: number, cap: number) =>
    cap > 0 ? Math.min(1, Math.max(0, kg / cap)) : 0;

  const fmtKg = (n: number) => n.toLocaleString(undefined, { maximumFractionDigits: 0 });

  return (
    <div className={styles.panel}>
      {/* ── FUEL SUMMARY ───────────────────────────────────────── */}
      <h2 className={styles.colHeading}>Fuel Summary</h2>
      <div className={styles.dataCard}>
        <div className={styles.capacity}>
          CAPACITY USABLE {fmtKg(f.fuelCapacityKg)} KG — SG: {f.specificGravity.toFixed(2)}
        </div>

        <div className={styles.capacityBar}>
          <div className={`${styles.capacityFill} ${capacityClass}`}
               style={{ width: `${capacityRatio * 100}%` }} />
          <span className={styles.capacityLabel}>
            {fmtKg(f.fuelInTanksKg)} / {fmtKg(f.fuelCapacityKg)} KG
          </span>
        </div>

        <div className={styles.summaryGrid}>
          <div className={styles.summaryHeader}>PLANNED (KG)</div>
          <div className={styles.summaryHeader}>IN TANKS (KG)</div>
          <div className={styles.summaryValue}>{fmtKg(f.plannedRampKg)}</div>
          <div className={styles.summaryValue}>{fmtKg(f.fuelInTanksKg)}</div>

          <div className={styles.summaryHeader}>PLANNED (L)</div>
          <div className={styles.summaryHeader}>IN TANKS (L)</div>
          <div className={styles.summaryValue}>{fmtKg(f.plannedRampLitres)}</div>
          <div className={styles.summaryValue}>{fmtKg(f.fuelInTanksLitres)}</div>
        </div>

        <div className={`${styles.deltaRow} ${deltaClass}`}>
          <span className={styles.deltaLabel}>DELTA:</span>
          <span className={styles.deltaValue}>
            {f.plannedRampKg > 0
              ? `${f.fuelDeltaKg >= 0 ? "+" : ""}${fmtKg(f.fuelDeltaKg)} KG`
              : "— (no flight plan)"}
          </span>
          {f.plannedRampKg > 0 && (
            <span className={styles.deltaTag}>
              {f.isOverFuelled ? "OVER" : f.isUnderFuelled ? "UNDER" : "OK"}
            </span>
          )}
        </div>
      </div>

      {/* ── TANK BREAKDOWN ─────────────────────────────────────── */}
      <h2 className={styles.colHeading}>Tank Breakdown</h2>
      <div className={styles.dataCard}>
        <TankRow
          label="CENTRE"
          kg={f.fuelCentreKg}
          capacityKg={f.fuelCentreCapacityKg}
          ratio={tankRatio(f.fuelCentreKg, f.fuelCentreCapacityKg)}
          fmtKg={fmtKg}
        />
        <TankRow
          label="LEFT"
          kg={f.fuelLeftKg}
          capacityKg={f.fuelLeftCapacityKg}
          ratio={tankRatio(f.fuelLeftKg, f.fuelLeftCapacityKg)}
          fmtKg={fmtKg}
        />
        <TankRow
          label="RIGHT"
          kg={f.fuelRightKg}
          capacityKg={f.fuelRightCapacityKg}
          ratio={tankRatio(f.fuelRightKg, f.fuelRightCapacityKg)}
          fmtKg={fmtKg}
        />
      </div>

      <p className={styles.note}>NOTE: THE ABOVE FIGURES ARE LIVE.</p>
    </div>
  );
}

interface TankRowProps {
  label: string;
  kg: number;
  capacityKg: number;
  ratio: number;
  fmtKg: (n: number) => string;
}

function TankRow({ label, kg, capacityKg, ratio, fmtKg }: TankRowProps) {
  return (
    <div className={styles.tankRow}>
      <div className={styles.tankLabel}>{label}</div>
      <div className={styles.tankBar}>
        <div className={styles.tankFill} style={{ width: `${ratio * 100}%` }} />
      </div>
      <div className={styles.tankValue}>
        {fmtKg(kg)} / {fmtKg(capacityKg)} KG
      </div>
    </div>
  );
}
