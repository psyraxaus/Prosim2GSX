import { useCallback, useEffect, useRef } from "react";
import { useApi } from "../api/useApi";
import {
  calculateLanding,
  getLanding,
  loadLandingRunways,
  postLandingInputs,
  resetLanding,
} from "../api/perf";
import { useAppState } from "../state/AppStateContext";
import type {
  LandingAutoMode,
  LandingBrakeMode,
  LandingFlapConfig,
  LandingInputsDto,
  LandingPerfStateDto,
  LandingRevMode,
} from "../types";
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
const SURFACE_OPTIONS: { code: number; label: string }[] = [
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
  const ld = state.landingPerf as unknown as LandingPerfStateDto | null;

  const pendingInputs = useRef<LandingInputsDto>({});
  const flushTimer = useRef<number | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const dto = await getLanding(api);
        if (!cancelled)
          dispatch({ type: "set", channel: "landingPerf", state: dto as unknown as Record<string, unknown> });
      } catch {
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
    if (Object.keys(payload).length === 0) return;
    try {
      await postLandingInputs(api, payload);
      const dto = await calculateLanding(api);
      dispatch({ type: "set", channel: "landingPerf", state: dto as unknown as Record<string, unknown> });
    } catch {
      /* WS will resync; server-side errors land on state.landingPerf.lastError */
    }
  }, [api, dispatch]);

  const setField = useCallback(<K extends keyof LandingInputsDto>(key: K, value: LandingInputsDto[K]) => {
    pendingInputs.current[key] = value;
    if (flushTimer.current !== null) window.clearTimeout(flushTimer.current);
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
    return <div className={`${sharedStyles.scope} ${styles.loading}`}>Loading landing performance…</div>;
  }

  const onIcaoCommit = (raw: string) => {
    const icao = raw.toUpperCase().trim().slice(0, 4);
    setField("icao", icao);
    if (icao.length === 4) {
      (async () => {
        try {
          await loadLandingRunways(api, icao);
          const dto = await calculateLanding(api);
          dispatch({ type: "set", channel: "landingPerf", state: dto as unknown as Record<string, unknown> });
        } catch { /* surfaced on next WS snapshot */ }
      })();
    }
  };

  const onReset = async () => {
    if (!window.confirm("Reset all landing perf inputs?")) return;
    try {
      const dto = await resetLanding(api);
      dispatch({ type: "set", channel: "landingPerf", state: dto as unknown as Record<string, unknown> });
    } catch { /* WS will resync */ }
  };

  // HW label flips on sign; abs value rendered. Server supplies the
  // colour class so WPF + web read identically.
  const hwSign = ld.hwKt < 0 ? "TW (KT)" : "HW (KT)";
  const hwAbs = Math.round(Math.abs(ld.hwKt));
  const xwAbs = Math.round(Math.abs(ld.xwKt));

  const showResult = ld.hasResult && !ld.isNoData && !ld.retreatFlap;

  return (
    <div className={`${sharedStyles.scope} ${styles.panel}`}>
      <div className={styles.header}>
        <span className={styles.title}>LANDING PERFORMANCE</span>
        <span className={styles.flightInfo}>{ld.icao || "----"}</span>
      </div>

      <div className={styles.body}>
        {/* ─── Column 1: Airport + runway-condition picker ──────────── */}
        <div className={styles.column}>
          <div className={styles.columnTitle}>Airport / Runway</div>

          <FieldRow label="ICAO">
            <input
              key={ld.icao}
              type="text"
              className={styles.icaoInput}
              defaultValue={ld.icao}
              maxLength={4}
              onBlur={(e) => onIcaoCommit(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") (e.target as HTMLInputElement).blur();
              }}
            />
          </FieldRow>

          <FieldRow label="Runway">
            <RunwayDropdown
              runways={ld.runways}
              runwayId={ld.runwayId}
              onRunwayChange={(rwy) => setField("runwayId", rwy)}
            />
          </FieldRow>

          <div className={styles.columnTitle} style={{ marginTop: 12 }}>Runway Condition</div>
          <div className={styles.surfaceStrip}>
            {SURFACE_OPTIONS.map((s) => (
              <button
                key={s.code}
                type="button"
                className={`${styles.surfaceBtn} ${s.code === ld.rwySurfaceCode ? styles.active : ""}`}
                onClick={() => setField("rwySurfaceCode", s.code)}
              >
                {s.label}
              </button>
            ))}
          </div>

          <div className={styles.columnTitle} style={{ marginTop: 12 }}>Wind / Weather</div>
          <FieldRow label="Wind">
            <div className={styles.windInputRow}>
              <input
                type="text"
                defaultValue={ld.windDir}
                onBlur={(e) => setField("windDir", e.target.value.toUpperCase().slice(0, 3))}
                maxLength={3}
                aria-label="Wind direction"
              />
              <span style={{ color: "var(--efb-text-dim)" }}>/</span>
              <KeyboardNumberInput
                value={ld.windKt}
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
              value={ld.oatC}
              onCommit={(n) => setField("oatC", n)}
              unit="°C"
              integer
              width={4}
              ariaLabel="OAT"
            />
          </FieldRow>
          <FieldRow label="QNH">
            <KeyboardNumberInput
              value={ld.qnhHpa}
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

        {/* ─── Column 2: Aircraft config ────────────────────────────── */}
        <div className={styles.column}>
          <div className={styles.columnTitle}>Aircraft Config</div>

          <FieldRow label="Ldg weight">
            <KeyboardNumberInput
              value={ld.ldgWeightTons}
              onCommit={(n) => setField("ldgWeightTons", n)}
              unit="t"
              width={5}
              min={0}
              ariaLabel="Landing weight"
            />
          </FieldRow>

          <FieldRow label="Flap">
            <ToggleGroup<LandingFlapConfig>
              value={ld.flapConfig}
              options={["FULL", "3"]}
              onChange={(v) => setField("flapConfig", v)}
            />
          </FieldRow>

          <FieldRow label="Brake">
            <ToggleGroup<LandingBrakeMode>
              value={ld.brakeMode}
              options={["LOW", "MED", "MAX"]}
              onChange={(v) => setField("brakeMode", v)}
            />
          </FieldRow>

          <FieldRow label="Reverse">
            <ToggleGroup<LandingRevMode>
              value={ld.revMode}
              options={["idle", "max"]}
              onChange={(v) => setField("revMode", v)}
            />
          </FieldRow>

          <FieldRow label="Autoland">
            <ToggleGroup<LandingAutoMode>
              value={ld.autolandMode}
              options={["manual", "auto"]}
              onChange={(v) => setField("autolandMode", v)}
            />
          </FieldRow>

          <FieldRow label="A/THR">
            <ToggleGroup<"0" | "1">
              value={ld.athr}
              options={["0", "1"]}
              onChange={(v) => setField("athr", v)}
            />
          </FieldRow>

          <FieldRow label="VAPP override">
            <KeyboardNumberInput
              value={ld.aircraftSpeedKt ?? 0}
              onCommit={(n) => setField("aircraftSpeedKt", n > 0 ? n : undefined)}
              unit="kt"
              integer
              width={4}
              min={0}
              ariaLabel="VAPP override"
            />
          </FieldRow>
        </div>

        {/* ─── Column 3: Output ─────────────────────────────────────── */}
        <div className={styles.column}>
          <div className={styles.columnTitle}>Output</div>

          {ld.isNoData && (
            <div className={`${styles.banner} ${styles.bannerWarn}`}>NO PERFORMANCE DATA</div>
          )}
          {ld.retreatFlap && (
            <div className={`${styles.banner} ${styles.bannerWarn}`}>RETREAT FLAP CONFIG</div>
          )}

          <div className={styles.outFrame}>
            <div className={styles.outRow}>
              <span className={styles.outLabel}>LDR</span>
              <span>
                <span className={`${styles.outValue} ${styles.outValueBig} ${valClass(ld.visualDistClass)}`}>
                  {showResult ? ld.ldrM : "----"}
                </span>
                <span className={styles.outUnit}>m</span>
              </span>
            </div>
            <div className={styles.outRow}>
              <span className={styles.outLabel}>LDR + 15%</span>
              <span>
                <span className={`${styles.outValue} ${valClass(ld.visualDistClass)}`}>
                  {showResult ? ld.ldr15M : "----"}
                </span>
                <span className={styles.outUnit}>m</span>
              </span>
            </div>
            <div className={styles.outRow}>
              <span className={styles.outLabel}>LDA</span>
              <span>
                <span className={`${styles.outValue} ${valClass(ld.visualDistClass)}`}>
                  {ld.ldaM > 0 ? Math.round(ld.ldaM) : "----"}
                </span>
                <span className={styles.outUnit}>m</span>
              </span>
            </div>
            <div className={styles.outRow}>
              <span className={styles.outLabel}>{hwSign}</span>
              <span className={`${styles.outValue} ${valClass(ld.hwClass)}`}>
                {showResult ? hwAbs : "----"}
              </span>
            </div>
            <div className={styles.outRow}>
              <span className={styles.outLabel}>XW (KT)</span>
              <span className={`${styles.outValue} ${valClass(ld.xwClass)}`}>
                {showResult ? xwAbs : "----"}
              </span>
            </div>
          </div>
        </div>
      </div>

      <div className={styles.footer}>
        <MetarStrip icao={ld.icao} metarText={ld.metarText} fetchedAt={ld.metarFetchedAt} />

        {ld.lastError && <div className={styles.errorRow}>{ld.lastError}</div>}

        <div className={styles.actions}>
          {/* v2 placeholder — failures tree not wired in v1 (D3). */}
          <button
            className={styles.actionBtn}
            disabled
            title="Failure cases arrive in a future release"
          >
            Failures…
          </button>
          <button
            className={`${styles.actionBtn} ${styles.actionFlex}`}
            onClick={onReset}
            disabled={ld.isBusy}
          >
            Reset
          </button>
          {ld.isBusy && <span className={styles.busy}>Working…</span>}
        </div>
      </div>
    </div>
  );
}

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
}) {
  return (
    <div className={styles.toggleRow}>
      {props.options.map((opt) => (
        <button
          key={opt}
          type="button"
          className={`${styles.toggleBtn} ${opt === props.value ? styles.active : ""}`}
          onClick={() => props.onChange(opt)}
        >
          {opt}
        </button>
      ))}
    </div>
  );
}

// Maps the server-supplied colour code to a CSS module class.
function valClass(code: string): string {
  if (code === "red") return styles.valRed;
  if (code === "red-margin") return styles.valRedMargin;
  return styles.valNormal;
}
