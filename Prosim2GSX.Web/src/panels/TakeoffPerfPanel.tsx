import { useCallback, useEffect, useRef, useState } from "react";
import { useApi } from "../api/useApi";
import {
  calculateTakeoff,
  getTakeoff,
  loadTakeoffRunways,
  postTakeoffInputs,
  resetTakeoff,
  syncTakeoffLoadsheet,
  uplinkTakeoff,
} from "../api/perf";
import { useAppState } from "../state/AppStateContext";
import type {
  LoadsheetSnapshotDto,
  TakeoffAntiIce,
  TakeoffFlap,
  TakeoffInputsDto,
  TakeoffPacks,
  TakeoffPerfStateDto,
  TakeoffSurface,
} from "../types";
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
  const to = state.takeoffPerf as unknown as TakeoffPerfStateDto | null;
  const ls = state.loadsheet as unknown as LoadsheetSnapshotDto | null;

  const [uplinkBadgeUntil, setUplinkBadgeUntil] = useState<number>(0);
  const pendingInputs = useRef<TakeoffInputsDto>({});
  const flushTimer = useRef<number | null>(null);

  // Initial REST fetch — seeds the channel so the panel can render
  // immediately even before the WS snapshot arrives.
  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const dto = await getTakeoff(api);
        if (!cancelled)
          dispatch({ type: "set", channel: "takeoffPerf", state: dto as unknown as Record<string, unknown> });
      } catch {
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
    if (Object.keys(payload).length === 0) return;
    try {
      const dto = await postTakeoffInputs(api, payload);
      dispatch({ type: "set", channel: "takeoffPerf", state: dto as unknown as Record<string, unknown> });
    } catch {
      /* WS will resync; surfaced via state.takeoffPerf.lastError if server-side */
    }
  }, [api, dispatch]);

  const setField = useCallback(<K extends keyof TakeoffInputsDto>(key: K, value: TakeoffInputsDto[K]) => {
    pendingInputs.current[key] = value;
    if (flushTimer.current !== null) window.clearTimeout(flushTimer.current);
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
    if (!to?.isUplinked) return;
    setUplinkBadgeUntil(Date.now() + UPLINK_BADGE_MS);
  }, [to?.isUplinked, to?.uplinkedAt]);

  useEffect(() => {
    if (uplinkBadgeUntil === 0) return;
    const remaining = uplinkBadgeUntil - Date.now();
    if (remaining <= 0) return;
    const id = window.setTimeout(() => setUplinkBadgeUntil(0), remaining);
    return () => window.clearTimeout(id);
  }, [uplinkBadgeUntil]);

  if (!to) {
    return <div className={`${sharedStyles.scope} ${styles.loading}`}>Loading takeoff performance…</div>;
  }

  const hasLoadsheet =
    (ls?.prelim?.status === "received") ||
    (ls?.final?.status === "received");

  const canCalculate =
    !!to.runwayId && to.towKg > 0 && to.mactowPercent > 0 && !to.isBusy;

  const canUplink =
    to.hasResult && to.v1 > 0 && to.vr > 0 && to.v2 > 0 && !to.isBusy;

  const uplinkActive = uplinkBadgeUntil > 0 && uplinkBadgeUntil > Date.now();

  const onIcaoCommit = (raw: string) => {
    const icao = raw.toUpperCase().trim().slice(0, 4);
    setField("icao", icao);
    // ICAO change triggers an immediate runway load (which loads METAR too).
    if (icao.length === 4) {
      (async () => {
        try {
          const dto = await loadTakeoffRunways(api, icao);
          dispatch({ type: "set", channel: "takeoffPerf", state: dto as unknown as Record<string, unknown> });
        } catch { /* surfaced on next WS snapshot */ }
      })();
    }
  };

  const onCalculate = async () => {
    try {
      const dto = await calculateTakeoff(api);
      dispatch({ type: "set", channel: "takeoffPerf", state: dto as unknown as Record<string, unknown> });
    } catch { /* WS will resync */ }
  };

  const onSyncLoadsheet = async () => {
    try {
      const dto = await syncTakeoffLoadsheet(api);
      dispatch({ type: "set", channel: "takeoffPerf", state: dto as unknown as Record<string, unknown> });
    } catch { /* WS will resync */ }
  };

  const onUplink = async () => {
    try {
      const dto = await uplinkTakeoff(api);
      dispatch({ type: "set", channel: "takeoffPerf", state: dto as unknown as Record<string, unknown> });
      setUplinkBadgeUntil(Date.now() + UPLINK_BADGE_MS);
    } catch { /* error surface lives on the state's lastError */ }
  };

  const onReset = async () => {
    if (!window.confirm("Reset all takeoff perf inputs?")) return;
    try {
      const dto = await resetTakeoff(api);
      dispatch({ type: "set", channel: "takeoffPerf", state: dto as unknown as Record<string, unknown> });
    } catch { /* WS will resync */ }
  };

  // Headwind/tailwind label flips on sign.
  const hwSign = to.hwCompKt < 0 ? "TW" : "HW";
  const hwAbs = Math.abs(to.hwCompKt);

  return (
    <div className={`${sharedStyles.scope} ${styles.panel}`}>
      <div className={styles.header}>
        <span className={styles.title}>TAKEOFF PERFORMANCE</span>
        <span className={styles.flightInfo}>
          ENG {to.engineVariant} · {to.icao || "----"}
        </span>
      </div>

      <div className={styles.body}>
        {/* ─── Column 1: Airport + Surface ───────────────────────────── */}
        <div className={styles.column}>
          <div className={styles.columnTitle}>Airport / Runway</div>

          <FieldRow label="ICAO">
            <input
              type="text"
              className={styles.icaoInput}
              defaultValue={to.icao}
              maxLength={4}
              onBlur={(e) => onIcaoCommit(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") (e.target as HTMLInputElement).blur();
              }}
            />
          </FieldRow>

          <FieldRow label="Runway">
            <RunwayDropdown
              runways={to.runways}
              runwayId={to.runwayId}
              intersectionName={to.intersectionName}
              withIntersections
              onRunwayChange={(rwy) => setField("runwayId", rwy)}
              onIntersectionChange={(name) => setField("intersectionName", name)}
            />
          </FieldRow>

          <FieldRow label="Surface">
            <ToggleGroup<TakeoffSurface>
              value={to.surface}
              options={["DRY", "WET"]}
              onChange={(v) => setField("surface", v)}
            />
          </FieldRow>

          <div className={styles.columnTitle} style={{ marginTop: 12 }}>Wind / Weather</div>

          <FieldRow label="Wind">
            <div className={styles.windInputRow}>
              <input
                type="text"
                defaultValue={to.windDir}
                onBlur={(e) => setField("windDir", e.target.value.toUpperCase().slice(0, 3))}
                maxLength={3}
                aria-label="Wind direction"
              />
              <span style={{ color: "var(--efb-text-dim)" }}>/</span>
              <KeyboardNumberInput
                value={to.windKt}
                onCommit={(n) => setField("windKt", n)}
                unit="kt"
                integer
                width={3}
                min={0}
                ariaLabel="Wind speed"
              />
            </div>
          </FieldRow>

          <FieldRow label="OAT">
            <KeyboardNumberInput
              value={to.oatC}
              onCommit={(n) => setField("oatC", n)}
              unit="°C"
              integer
              width={4}
              ariaLabel="OAT"
            />
          </FieldRow>

          <FieldRow label="QNH">
            <KeyboardNumberInput
              value={to.qnhHpa}
              onCommit={(n) => setField("qnhHpa", n)}
              unit="hPa"
              integer
              width={5}
              min={900}
              max={1100}
              ariaLabel="QNH"
            />
          </FieldRow>
        </div>

        {/* ─── Column 2: Aircraft configuration ─────────────────────── */}
        <div className={styles.column}>
          <div className={styles.columnTitle}>Aircraft Config</div>

          <FieldRow label="Flap">
            <ToggleGroup<TakeoffFlap>
              value={to.flap}
              options={["opt", "1+F", "2", "3"]}
              onChange={(v) => setField("flap", v)}
            />
          </FieldRow>

          <FieldRow label="Anti-ice">
            <ToggleGroup<TakeoffAntiIce>
              value={to.antiIce}
              options={["OFF", "ENG", "ENG+WING"]}
              onChange={(v) => setField("antiIce", v)}
            />
          </FieldRow>

          <FieldRow label="Packs">
            <ToggleGroup<TakeoffPacks>
              value={to.packs}
              options={["OFF", "ON"]}
              onChange={(v) => setField("packs", v)}
            />
          </FieldRow>

          <FieldRow label="Force TOGA">
            <label className={styles.checkRow}>
              <input
                type="checkbox"
                checked={to.forceToga}
                onChange={(e) => setField("forceToga", e.target.checked)}
              />
            </label>
          </FieldRow>

          <div className={styles.columnTitle} style={{ marginTop: 12 }}>Weights</div>

          <FieldRow label="TOW">
            <KeyboardNumberInput
              value={to.towKg}
              onCommit={(n) => setField("towKg", n)}
              unit="kg"
              integer
              width={7}
              min={0}
              ariaLabel="TOW"
            />
          </FieldRow>

          <FieldRow label="MAC TOW">
            <KeyboardNumberInput
              value={to.mactowPercent}
              onCommit={(n) => setField("mactowPercent", n)}
              unit="%"
              width={5}
              min={0}
              ariaLabel="MAC TOW"
            />
          </FieldRow>
        </div>

        {/* ─── Column 3: Output / MCDU display ──────────────────────── */}
        <div className={styles.column}>
          <div className={styles.columnTitle}>Output</div>

          <div className={styles.mcduFrame}>
            <div className={styles.mcduTitle}>FMGC TAKE OFF</div>
            <McduRow label="V1"        value={to.hasResult ? to.v1 : null} />
            <McduRow label="VR"        value={to.hasResult ? to.vr : null} />
            <McduRow label="V2"        value={to.hasResult ? to.v2 : null} />
            <McduRow label="FLAPS"     value={to.hasResult ? confLabel(to.flapSettings) : null} />
            <McduRow
              label="FLEX"
              value={to.hasResult ? (to.flexOutputC === 0 ? "TOGA" : `${to.flexOutputC}°`) : null}
              amber={to.hasResult && to.flexOutputC === 0}
            />
            <McduRow
              label="THS"
              value={
                to.hasResult
                  ? `${to.trimDir || (to.thsValue >= 0 ? "UP" : "DN")} ${Math.abs(to.thsValue).toFixed(1)}`
                  : null
              }
            />
            <McduRow label="SHIFT"     value={to.hasResult ? `${to.shiftM}` : null} />
          </div>

          <div className={styles.columnTitle} style={{ marginTop: 12 }}>Limits</div>
          <FieldRow label={`${hwSign} component`}>
            <span className={to.hasResult ? styles.mcduValue : styles.mcduValueDim}>
              {to.hasResult ? `${hwAbs} kt` : "----"}
            </span>
          </FieldRow>
          <FieldRow label="TOPL">
            <span className={to.toplLimited ? styles.mcduValueAmber : styles.mcduValue}>
              {to.hasResult ? `${Math.round(to.toplKg)} kg` : "----"}
            </span>
          </FieldRow>
          {to.greenDot !== null && (
            <FieldRow label="Green dot">
              <span className={styles.mcduValue}>{to.greenDot} kt</span>
            </FieldRow>
          )}
        </div>
      </div>

      <div className={styles.footer}>
        <MetarStrip icao={to.icao} metarText={to.metarText} fetchedAt={to.metarFetchedAt} />

        {to.calculationError && (
          <div className={`${styles.banner} ${styles.bannerError}`}>{to.calculationError}</div>
        )}
        {to.lastError && (
          <div className={styles.errorRow}>{to.lastError}</div>
        )}
        {to.hasResult && to.toplLimited && (
          <div className={`${styles.banner} ${styles.bannerWarn}`}>*** TOPL LIMITED ***</div>
        )}
        {to.hasResult && to.forceTogaResult && (
          <div className={`${styles.banner} ${styles.bannerWarn}`}>*** TOGA REQUIRED ***</div>
        )}

        <div className={styles.actions}>
          {hasLoadsheet && (
            <button className={styles.actionBtn} onClick={onSyncLoadsheet} disabled={to.isBusy}>
              Sync Loadsheet
            </button>
          )}
          <button
            className={`${styles.actionBtn} ${styles.actionPrimary}`}
            onClick={onCalculate}
            disabled={!canCalculate}
          >
            Calculate
          </button>
          <button
            className={`${styles.actionBtn} ${uplinkActive ? styles.actionSent : ""}`}
            onClick={onUplink}
            disabled={!canUplink || uplinkActive}
          >
            {uplinkActive ? "Uplink Sent" : "Send Uplink"}
          </button>
          <button className={`${styles.actionBtn} ${styles.actionFlex}`} onClick={onReset} disabled={to.isBusy}>
            Reset
          </button>
          {to.isBusy && <span className={styles.busy}>Working…</span>}
        </div>
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────
//  Small layout helpers (panel-local; the perf-shared module has the
//  cross-panel pieces).
// ─────────────────────────────────────────────────────────────────────

function FieldRow({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className={styles.fieldRow}>
      <span className={styles.fieldLabel}>{label}</span>
      <span className={styles.fieldValue}>{children}</span>
    </div>
  );
}

function ToggleGroup<T extends string>(props: {
  value: T;
  options: T[];
  onChange: (v: T) => void;
  disabled?: boolean;
}) {
  return (
    <div className={styles.toggleRow}>
      {props.options.map((opt) => (
        <button
          key={opt}
          type="button"
          className={`${styles.toggleBtn} ${opt === props.value ? styles.active : ""}`}
          disabled={props.disabled}
          onClick={() => props.onChange(opt)}
        >
          {opt}
        </button>
      ))}
    </div>
  );
}

function McduRow({ label, value, amber }: { label: string; value: string | number | null; amber?: boolean }) {
  const cls = value === null
    ? styles.mcduValueDim
    : amber
      ? styles.mcduValueAmber
      : styles.mcduValue;
  return (
    <div className={styles.mcduRow}>
      <span className={styles.mcduLabel}>{label}</span>
      <span className={cls}>{value === null ? "----" : value}</span>
    </div>
  );
}

function confLabel(flapSettings: number): string {
  if (flapSettings === 1) return "1+F";
  if (flapSettings === 2) return "2";
  if (flapSettings === 3) return "3";
  return "—";
}
