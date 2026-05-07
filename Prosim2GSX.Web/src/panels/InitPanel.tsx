import { useEffect, useState } from "react";
import { useApi } from "../api/useApi";
import { useAppState } from "../state/AppStateContext";
import {
  EfbFlightPlanDto,
  FetchOfpRequest,
  OFPData,
  OfpSource,
} from "../types";
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
  const [busy, setBusy] = useState<"fetch" | "sync" | "clear" | "reset" | null>(null);
  const [resetArmed, setResetArmed] = useState(false);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const dto = await get<EfbFlightPlanDto>("/efb/flight-plan");
        if (!cancelled) {
          dispatch({
            type: "set",
            channel: "efbFlightPlan",
            state: dto as unknown as Record<string, unknown>,
          });
        }
      } catch {
        /* WS will fill in once connected; useApi already handled 401 */
      }
    })();
    return () => { cancelled = true; };
  }, [get, dispatch]);

  const efb = state.efbFlightPlan as unknown as EfbFlightPlanDto | null;
  if (!efb) {
    return <div className={styles.loading}>LOADING INIT…</div>;
  }

  const ofp = efb.ofp;

  const onFetch = async () => {
    setBusy("fetch");
    try {
      const req: FetchOfpRequest = {
        departure: ofp?.departureIcao ?? "",
        arrival: ofp?.arrivalIcao ?? "",
        alternate: ofp?.alternateIcao ?? "",
        flightNumber: ofp?.flightNumber ?? "",
      };
      await post("/efb/fetch-ofp", req);
    } catch {
      /* lastFetchError surfaced in the broadcast */
    } finally {
      setBusy(null);
    }
  };

  const onSync = async () => {
    setBusy("sync");
    try { await post("/efb/sync-to-fms"); }
    catch { /* server logs */ }
    finally { setBusy(null); }
  };

  const onClearOverrides = async () => {
    setBusy("clear");
    try { await post("/efb/clear-all-overrides"); }
    catch { /* server logs */ }
    finally { setBusy(null); }
  };

  const onReset = async () => {
    if (!resetArmed) {
      setResetArmed(true);
      window.setTimeout(() => setResetArmed(false), 3000);
      return;
    }
    setResetArmed(false);
    setBusy("reset");
    try { await post("/efb/reset-flight"); }
    catch { /* server logs */ }
    finally { setBusy(null); }
  };

  const hasOverrides = Object.values(efb.overrideFlags ?? {}).some((v) => v);

  return (
    <div className={styles.panel}>
      <div className={styles.inner}>
      <div className={styles.columns}>
        {/* LEFT — ACTIVE / INIT */}
        <div className={styles.column}>
          <div className={styles.title}>ACTIVE / INIT</div>
          <div className={styles.separator} />

          <Row label="FLT NBR" value={ofp?.flightNumber} />

          <div className={styles.rowSplit}>
            <span className={styles.label}>FROM</span>
            <Value text={ofp?.departureIcao} />
            <span className={styles.label}>TO</span>
            <Value text={ofp?.arrivalIcao} />
            <span className={styles.label}>ALTN</span>
            <Value text={ofp?.alternateIcao} />
          </div>

          <Row label="RWY OUT/IN" value={formatRwys(ofp)} />
          <Row label="CALLSIGN" value={ofp?.callsign} />

          <div className={styles.separator} />

          <Row label="CRZ FL" value={formatFl(ofp?.cruiseFlightLevel)} />
          <Row label="CI" value={formatCi(ofp?.costIndex)} />
          <Row label="CPNY RTE" value={ofp?.route} />

          <div className={styles.separator} />

          <Row label="STD" value={formatZulu(ofp?.std)} />
          <Row label="ETA" value={formatZulu(ofp?.eta)} />

          <div className={styles.separator} />

          <StatusLine efb={efb} />
          {efb.lastFetchError && <div className={styles.errorLine}>{efb.lastFetchError}</div>}
        </div>

        {/* RIGHT — DATA / STATUS */}
        <div className={styles.column}>
          <div className={styles.title}>DATA / STATUS</div>
          <div className={styles.separator} />

          <Row label="ACFT" value={ofp?.aircraftType} />
          <Row label="REG" value={ofp?.aircraftReg} />

          <div className={styles.separator} />

          <FuelRow label="ZFW"        value={formatKg(effective(efb, "zfwKg", ofp?.zfwKg))}        overridden={isOverridden(efb, "zfwKg")} />
          <FuelRow label="OEW"        value={formatKg(ofp?.oewKg)} />
          <FuelRow label="FUEL RAMP"  value={formatKg(effective(efb, "fuelRampKg", ofp?.fuelRampKg))} overridden={isOverridden(efb, "fuelRampKg")} />
          <FuelRow label="FUEL TRIP"  value={formatKg(ofp?.fuelTripKg)} />
          <FuelRow label="FUEL MIN"   value={formatKg(ofp?.fuelMinimumKg)} />
          <FuelRow label="FUEL EXTRA" value={formatKg(ofp?.fuelExtraKg)} />
          <FuelRow label="PAX"        value={formatPax(effective(efb, "passengerCount", ofp?.passengerCount))} overridden={isOverridden(efb, "passengerCount")} />
          <FuelRow label="CARGO"      value={formatKg(effective(efb, "cargoKg", ofp?.cargoKg))}    overridden={isOverridden(efb, "cargoKg")} />

          <div className={styles.separator} />
          <div className={styles.fetchedAt}>{formatFetchedAt(efb.fetchedAt)}</div>
        </div>
      </div>

      {/* ACTIONS — full-width row spanning both columns */}
      <div className={styles.actions}>
        <button
          type="button"
          className={`${styles.btn} ${styles.btnPrimary}`}
          onClick={onFetch}
          disabled={busy !== null || efb.isBusy}
        >
          {busy === "fetch" || efb.isBusy ? "FETCHING…" : "FETCH OFP"}
        </button>
        <button
          type="button"
          className={`${styles.btn} ${styles.btnPrimary}`}
          onClick={onSync}
          disabled={busy !== null || !efb.isOfpLoaded}
        >
          {busy === "sync" ? "SYNCING…" : "SYNC TO FMS"}
        </button>
        <button
          type="button"
          className={`${styles.btn} ${styles.btnAmber}`}
          onClick={onClearOverrides}
          disabled={busy !== null || !hasOverrides}
        >
          {busy === "clear" ? "CLEARING…" : "CLEAR OVERRIDES"}
        </button>
        <button
          type="button"
          className={`${styles.btn} ${styles.btnWarn}`}
          onClick={onReset}
          disabled={busy !== null}
        >
          {busy === "reset" ? "RESETTING…" : resetArmed ? "CONFIRM RESET" : "RESET FLIGHT"}
        </button>
      </div>
      </div>
    </div>
  );
}

// ── Sub-components ─────────────────────────────────────────────────────────

function Row({ label, value }: { label: string; value: string | undefined }) {
  return (
    <div className={styles.row}>
      <span className={styles.label}>{label}</span>
      <Value text={value} />
    </div>
  );
}

function FuelRow({ label, value, overridden }: { label: string; value: string; overridden?: boolean }) {
  const valueClass = `${styles.valueRight} ${overridden ? styles.valueOverridden : ""}`;
  return (
    <div className={styles.rowFuel}>
      <span className={styles.label}>{label}</span>
      <span className={valueClass}>{value}</span>
    </div>
  );
}

function Value({ text }: { text: string | undefined }) {
  const t = (text ?? "").trim();
  if (!t) return <span className={styles.value}>—</span>;
  return <span className={styles.value}>{t}</span>;
}

function StatusLine({ efb }: { efb: EfbFlightPlanDto }) {
  const { dot, label } = statusFor(efb);
  return (
    <div className={styles.statusRow}>
      <span className={`${styles.statusDot} ${dot}`} />
      <span className={styles.value}>{label}</span>
      <span className={styles.label}>{sourceLabel(efb.source)}</span>
    </div>
  );
}

// ── Helpers ────────────────────────────────────────────────────────────────

function statusFor(efb: EfbFlightPlanDto): { dot: string; label: string } {
  if (efb.isBusy) return { dot: styles.statusDotCyan, label: "FETCHING…" };
  if (efb.lastFetchError) return { dot: styles.statusDotWarn, label: "UPLINK FAILED" };
  if (efb.status === "Loaded") return { dot: styles.statusDotGreen, label: "OFP LOADED" };
  return { dot: styles.statusDotAmber, label: "AWAITING OFP" };
}

function sourceLabel(source: OfpSource): string {
  switch (source) {
    case "SimbriefEfb": return "SIMBRIEF";
    case "Mcdu": return "MCDU";
    case "Manual": return "MANUAL";
    default: return "—";
  }
}

function isOverridden(efb: EfbFlightPlanDto, field: string): boolean {
  return !!efb.overrideFlags?.[field];
}

function effective(efb: EfbFlightPlanDto, field: string, ofpFallback: number | undefined): number {
  if (!isOverridden(efb, field)) return ofpFallback ?? 0;
  const v = efb.overrideValues?.[field];
  if (typeof v === "number") return v;
  if (typeof v === "string") {
    const n = parseFloat(v);
    return Number.isFinite(n) ? n : (ofpFallback ?? 0);
  }
  return ofpFallback ?? 0;
}

function formatKg(kg: number | undefined): string {
  if (!kg || kg <= 0) return "—";
  return `${Math.round(kg).toLocaleString("en-US")} KG`;
}

function formatPax(n: number): string {
  return n > 0 ? Math.round(n).toString() : "—";
}

function formatFl(fl: number | undefined): string {
  return fl && fl > 0 ? `FL${fl.toString().padStart(3, "0")}` : "FL---";
}

function formatCi(ci: number | undefined): string {
  return ci && ci > 0 ? ci.toString() : "—";
}

function formatZulu(iso: string | null | undefined): string {
  if (!iso) return "—";
  const d = new Date(iso);
  if (isNaN(d.getTime())) return "—";
  const hh = String(d.getUTCHours()).padStart(2, "0");
  const mm = String(d.getUTCMinutes()).padStart(2, "0");
  return `${hh}${mm}Z`;
}

function formatRwys(ofp: OFPData | null | undefined): string {
  const dep = (ofp?.departurePlanRwy ?? "").trim();
  const arr = (ofp?.arrivalPlanRwy ?? "").trim();
  if (!dep && !arr) return "—";
  return `${dep || "—"} / ${arr || "—"}`;
}

function formatFetchedAt(iso: string | null): string {
  if (!iso) return "";
  const d = new Date(iso);
  if (isNaN(d.getTime())) return "";
  const hh = String(d.getHours()).padStart(2, "0");
  const mm = String(d.getMinutes()).padStart(2, "0");
  const ss = String(d.getSeconds()).padStart(2, "0");
  return `FETCHED ${hh}:${mm}:${ss}`;
}
