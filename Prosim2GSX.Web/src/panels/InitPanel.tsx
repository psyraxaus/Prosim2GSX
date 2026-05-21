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
// The four writable rows (ZFW / FUEL RAMP / PAX / CARGO) are click-to-
// edit when LockFieldsFromOfp is true: the value cell turns into an
// inline input with ENT / CLR keys. Range-validated against the bounds
// in FIELD_RANGES below; out-of-range commits flash a red "OUT OF
// RANGE" hint and reject without writing.

// Sensible per-field bounds for override validation. Loose enough to
// allow non-A320 airframes, tight enough to catch typos like an extra
// digit.
const FIELD_RANGES: Record<string, { min: number; max: number; integer?: boolean }> = {
  zfwKg:          { min: 30000, max: 70000 },
  fuelRampKg:     { min:     0, max: 25000 },
  passengerCount: { min:     0, max:   220, integer: true },
  cargoKg:        { min:     0, max:  8000 },
};

export function InitPanel() {
  const { get, post } = useApi();
  const { state, dispatch } = useAppState();
  const [busy, setBusy] = useState<"fetch" | "sync" | "clear" | "reset" | null>(null);
  const [resetArmed, setResetArmed] = useState(false);

  // Inline-editor state. Only one row is ever in edit mode at a time;
  // committing or cancelling clears all three.
  const [editingField, setEditingField] = useState<string | null>(null);
  const [editValue, setEditValue] = useState("");
  const [editError, setEditError] = useState<string | null>(null);

  // Fake-phased fetch indicator — the server-side fetch is monolithic
  // (one HTTP call, one broadcast), but the prompt's spec calls for the
  // text to cycle through phases so the user sees something happening.
  // Cleared when isBusy goes false.
  const [fetchPhase, setFetchPhase] = useState("");

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

  const startEdit = (field: string, currentValue: number) => {
    if (!efb.isOfpLoaded) return;
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
    if (!field) return;
    const range = FIELD_RANGES[field];
    if (!range) { cancelEdit(); return; }

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
    } catch {
      flashError("WRITE FAILED");
    }
  };

  const flashError = (msg: string) => {
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

  return (
    <div className={styles.panel}>
      <div className={styles.inner}>
      <div className={styles.columns}>
        {/* LEFT — ACTIVE / INIT */}
        <div className={styles.column}>
          <div className={styles.title}>ACTIVE / INIT</div>
          <div className={styles.separator} />

          <Row label="FLT NBR" value={ofp?.flightNumber} pending={pending} pendingLen={6} />

          <div className={styles.rowSplit}>
            <span className={styles.label}>FROM</span>
            <Value text={ofp?.departureIcao} pending={pending} pendingLen={4} />
            <span className={styles.label}>TO</span>
            <Value text={ofp?.arrivalIcao} pending={pending} pendingLen={4} />
            <span className={styles.label}>ALTN</span>
            <Value text={ofp?.alternateIcao} pending={pending} pendingLen={4} />
          </div>

          <Row label="RWY OUT/IN" value={formatRwys(ofp)} pending={pending} pendingLen={5} />
          <Row label="CALLSIGN" value={ofp?.callsign} pending={pending} pendingLen={6} />

          <div className={styles.separator} />

          <Row label="CRZ FL" value={formatFl(ofp?.cruiseFlightLevel)} pending={pending} pendingLen={5} />
          <Row label="CI" value={formatCi(ofp?.costIndex)} pending={pending} pendingLen={3} />
          <Row label="CPNY RTE" value={ofp?.route} pending={pending} pendingLen={10} />

          <div className={styles.separator} />

          <Row label="STD" value={formatZulu(ofp?.std)} pending={pending} pendingLen={4} />
          <Row label="ETA" value={formatZulu(ofp?.eta)} pending={pending} pendingLen={4} />

          <div className={styles.separator} />

          <StatusLine efb={efb} />
          {efb.lastFetchError && <div className={styles.errorLine}>{efb.lastFetchError}</div>}

          {fetchPhase && <div className={styles.fetchPhase}>{fetchPhase}</div>}
        </div>

        {/* RIGHT — DATA / STATUS */}
        <div className={styles.column}>
          <div className={styles.title}>DATA / STATUS</div>
          <div className={styles.separator} />

          <Row label="ACFT" value={ofp?.aircraftType} pending={pending} pendingLen={8} />
          <Row label="REG" value={ofp?.aircraftReg} pending={pending} pendingLen={6} />

          <div className={styles.separator} />

          <EditableFuelRow
            label="ZFW" field="zfwKg"
            value={formatKg(effective(efb, "zfwKg", ofp?.zfwKg))}
            rawValue={effective(efb, "zfwKg", ofp?.zfwKg)}
            overridden={isOverridden(efb, "zfwKg")}
            editable={editable && efb.isOfpLoaded} pending={pending}
            editingField={editingField} editValue={editValue} editError={editError}
            onStart={startEdit} onChange={setEditValue}
            onCommit={commitEdit} onCancel={cancelEdit}
          />
          <FuelRow label="OEW" value={formatKg(ofp?.oewKg)} pending={pending} />
          <EditableFuelRow
            label="FUEL RAMP" field="fuelRampKg"
            value={formatKg(effective(efb, "fuelRampKg", ofp?.fuelRampKg))}
            rawValue={effective(efb, "fuelRampKg", ofp?.fuelRampKg)}
            overridden={isOverridden(efb, "fuelRampKg")}
            editable={editable && efb.isOfpLoaded} pending={pending}
            editingField={editingField} editValue={editValue} editError={editError}
            onStart={startEdit} onChange={setEditValue}
            onCommit={commitEdit} onCancel={cancelEdit}
          />
          <FuelRow label="FUEL TRIP"  value={formatKg(ofp?.fuelTripKg)}    pending={pending} />
          <FuelRow label="FUEL MIN"   value={formatKg(ofp?.fuelMinimumKg)} pending={pending} />
          <FuelRow label="FUEL EXTRA" value={formatKg(ofp?.fuelExtraKg)}   pending={pending} />
          <EditableFuelRow
            label="PAX" field="passengerCount"
            value={formatPax(effective(efb, "passengerCount", ofp?.passengerCount))}
            rawValue={effective(efb, "passengerCount", ofp?.passengerCount)}
            overridden={isOverridden(efb, "passengerCount")}
            editable={editable && efb.isOfpLoaded} pending={pending}
            editingField={editingField} editValue={editValue} editError={editError}
            onStart={startEdit} onChange={setEditValue}
            onCommit={commitEdit} onCancel={cancelEdit}
          />
          <EditableFuelRow
            label="CARGO" field="cargoKg"
            value={formatKg(effective(efb, "cargoKg", ofp?.cargoKg))}
            rawValue={effective(efb, "cargoKg", ofp?.cargoKg)}
            overridden={isOverridden(efb, "cargoKg")}
            editable={editable && efb.isOfpLoaded} pending={pending}
            editingField={editingField} editValue={editValue} editError={editError}
            onStart={startEdit} onChange={setEditValue}
            onCommit={commitEdit} onCancel={cancelEdit}
          />

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

function Row({ label, value, pending, pendingLen }: { label: string; value: string | undefined; pending?: boolean; pendingLen?: number }) {
  return (
    <div className={styles.row}>
      <span className={styles.label}>{label}</span>
      <Value text={value} pending={pending} pendingLen={pendingLen} />
    </div>
  );
}

function FuelRow({ label, value, overridden, pending }: { label: string; value: string; overridden?: boolean; pending?: boolean }) {
  if (pending) {
    return (
      <div className={styles.rowFuel}>
        <span className={styles.label}>{label}</span>
        <PendingBoxes count={5} fuelRow />
      </div>
    );
  }
  const valueClass = `${styles.valueRight} ${overridden ? styles.valueOverridden : ""}`;
  return (
    <div className={styles.rowFuel}>
      <span className={styles.label}>{label}</span>
      <span className={valueClass}>{value}</span>
    </div>
  );
}

interface EditableFuelRowProps {
  label: string;
  field: string;
  value: string;
  rawValue: number;
  overridden: boolean;
  editable: boolean;
  pending?: boolean;
  editingField: string | null;
  editValue: string;
  editError: string | null;
  onStart: (field: string, currentValue: number) => void;
  onChange: (next: string) => void;
  onCommit: () => void;
  onCancel: () => void;
}

// Click-to-edit row for the four writable fields. Display mode shows the
// effective value with an optional MOD chip; click flips to edit mode
// with an inline input + ENT / CLR keys. The error message ("OUT OF
// RANGE", "INVALID", "WRITE FAILED") sits next to the keys for ~2s when
// a commit is rejected.
function EditableFuelRow(props: EditableFuelRowProps) {
  const isEditing = props.editingField === props.field;
  const error = isEditing ? props.editError : null;

  if (props.pending) {
    return (
      <div className={styles.rowFuel}>
        <span className={styles.label}>{props.label}</span>
        <PendingBoxes count={5} fuelRow />
      </div>
    );
  }

  if (isEditing) {
    return (
      <div className={styles.rowFuel}>
        <span className={styles.label}>{props.label}</span>
        <div className={styles.editor}>
          <input
            className={styles.editorInput}
            type="number"
            inputMode="numeric"
            value={props.editValue}
            onChange={(e) => props.onChange(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") { e.preventDefault(); props.onCommit(); }
              else if (e.key === "Escape") { e.preventDefault(); props.onCancel(); }
            }}
            autoFocus
          />
          <button
            type="button"
            className={`${styles.editorKey} ${styles.editorKeyEnter}`}
            onClick={props.onCommit}
            title="Commit override (Enter)"
          >ENT</button>
          <button
            type="button"
            className={`${styles.editorKey} ${styles.editorKeyClr}`}
            onClick={props.onCancel}
            title="Cancel (Esc)"
          >CLR</button>
          {error && <span className={styles.editorError}>{error}</span>}
        </div>
      </div>
    );
  }

  const valueClass = `${styles.valueRight} ${props.overridden ? styles.valueOverridden : ""}`;
  const rowClass = `${styles.rowFuel} ${props.editable ? styles.rowFuelEditable : ""}`;
  return (
    <div
      className={rowClass}
      onClick={props.editable ? () => props.onStart(props.field, props.rawValue) : undefined}
      title={props.editable ? "Click to override" : undefined}
    >
      <span className={styles.label}>{props.label}</span>
      <span className={valueClass}>{props.value}</span>
      {props.overridden && <span className={styles.modChip}>MOD</span>}
    </div>
  );
}

function Value({ text, pending, pendingLen }: { text: string | undefined; pending?: boolean; pendingLen?: number }) {
  if (pending) {
    return <PendingBoxes count={pendingLen ?? 4} />;
  }
  const t = (text ?? "").trim();
  if (!t) return <span className={styles.value}>—</span>;
  return <span className={styles.value}>{t}</span>;
}

// Row of amber-outlined empty boxes — one per character slot. Used as
// the "awaiting entry" placeholder on every OFP-derived field while no
// OFP is loaded. Static (no blink) — matches the real FMS INIT page.
function PendingBoxes({ count, fuelRow }: { count: number; fuelRow?: boolean }) {
  const n = Math.max(1, count);
  const cls = fuelRow ? `${styles.pending} ${styles.pendingFuel}` : styles.pending;
  return (
    <span className={cls}>
      {Array.from({ length: n }).map((_, i) => (
        <span key={i} className={styles.pendingBox} />
      ))}
    </span>
  );
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
