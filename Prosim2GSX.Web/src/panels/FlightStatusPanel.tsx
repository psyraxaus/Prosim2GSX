import { useEffect, useRef } from "react";
import { useApi } from "../api/useApi";
import { useAppState } from "../state/AppStateContext";
import { AutomationState, FlightStatusDto, GsxServiceState } from "../types";
import styles from "./FlightStatusPanel.module.css";

// Read-only Monitor surface. Initial REST load on mount; live updates via
// the WebSocket "flightStatus" + "gsx" channels (already merged into
// AppStateContext by the WS hook).
export function FlightStatusPanel() {
  const { get } = useApi();
  const { state, dispatch } = useAppState();

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const dto = await get<FlightStatusDto>("/flightstatus");
        if (!cancelled) dispatch({ type: "set", channel: "flightStatus", state: dto as unknown as Record<string, unknown> });
      } catch {
        /* useApi already handled 401; transient errors are fine — WS will fill in */
      }
    })();
    return () => { cancelled = true; };
  }, [get, dispatch]);

  const fs = state.flightStatus as unknown as FlightStatusDto | null;
  if (!fs) {
    return <div className={styles.loading}>Loading flight status…</div>;
  }

  return (
    <div className={styles.panel}>
      <FlightPhaseBox phase={fs.gsx.appAutomationState} />

      <Section title="Sim">
        <Indicator label="Sim Running" ok={fs.simRunning} />
        <Indicator label="Sim Connected" ok={fs.simConnected} />
        <Indicator label="Sim Session" ok={fs.simSession} />
        <Indicator label="Sim Paused" ok={!fs.simPaused} reverseLabel="Paused" />
        <Indicator label="Walkaround" ok={!fs.simWalkaround} reverseLabel="Active" />
        <KV label="Camera State" value={String(fs.cameraState)} />
        <KV label="Sim Version" value={fs.simVersion || "—"} />
        <KV label="Aircraft" value={fs.aircraftString || "—"} />
      </Section>

      <Section title="App">
        <Indicator label="GSX Controller" ok={fs.appGsxController} />
        <Indicator label="Aircraft Binary" ok={fs.appAircraftBinary} />
        <Indicator label="Aircraft Interface" ok={fs.appAircraftInterface} />
        <Indicator label="ProSim Connected" ok={fs.appProsimConnected} />
        <Indicator label="ProSim SDK Connected" ok={fs.appProsimSdkConnected} />
        <Indicator label="Automation Controller" ok={fs.appAutomationController} />
        <Indicator label="Audio Controller" ok={fs.appAudioController} />
        <KV label="Profile" value={fs.appProfile || "—"} />
        <KV label="Aircraft" value={fs.appAircraft} />
        <KV label="On Ground" value={String(fs.appOnGround)} />
        <KV label="Engines Running" value={String(fs.appEnginesRunning)} />
        <KV label="In Motion" value={String(fs.appInMotion)} />
      </Section>

      <Section title="GSX">
        <Indicator label="GSX Running" ok={fs.gsx.gsxRunning} />
        <Indicator label="Couatl Vars" ok={fs.gsx.gsxStartedValid} />
        <KV label="Couatl Started" value={fs.gsx.gsxStarted} />
        <KV label="Menu State" value={fs.gsx.gsxMenu} />
        <KV label="Pax Target" value={String(fs.gsx.gsxPaxTarget)} />
        <KV label="Pax Total (B|D)" value={fs.gsx.gsxPaxTotal} />
        <KV label="Cargo (B|D)" value={fs.gsx.gsxCargoProgress} />
        <KV label="Departure Services" value={fs.gsx.appAutomationDepartureServices} />
      </Section>

      <Section title="Services">
        <ServiceRow label="Reposition" state={fs.gsx.serviceReposition} />
        <ServiceRow label="Refuel" state={fs.gsx.serviceRefuel} />
        <ServiceRow label="Catering" state={fs.gsx.serviceCatering} />
        <ServiceRow label="Lavatory" state={fs.gsx.serviceLavatory} />
        <ServiceRow label="Water" state={fs.gsx.serviceWater} />
        <ServiceRow label="Cleaning" state={fs.gsx.serviceCleaning} />
        <GpuRow connected={fs.gsx.serviceGpuConnected} relevant={fs.gsx.serviceGpuPhaseRelevant} />
        <ServiceRow label="Boarding" state={fs.gsx.serviceBoarding} />
        <ServiceRow label="Deboarding" state={fs.gsx.serviceDeboarding} />
        <KV label="Pushback" value={fs.gsx.servicePushback} />
        <ServiceRow label="Jetway" state={fs.gsx.serviceJetway} />
        <ServiceRow label="Stairs" state={fs.gsx.serviceStairs} />
      </Section>

      <Section title="Log">
        <LogTail messages={fs.messageLog} />
      </Section>
    </div>
  );
}

// Mirrors ViewMonitor.xaml's FLIGHT PHASE section: a large centred phase
// label above a 7-block progress bar (PREFLIGHT / DEPARTURE / PUSHBACK /
// TAXI OUT / FLIGHT / TAXI IN / ARRIVAL). PREFLIGHT covers SessionStart and
// Preparation; ARRIVAL covers Arrival and TurnAround.
type PhaseBlock = { label: string; states: AutomationState[] };
const PHASE_BLOCKS: PhaseBlock[] = [
  { label: "PREFLIGHT", states: ["SessionStart", "Preparation"] },
  { label: "DEPARTURE", states: ["Departure"] },
  { label: "PUSHBACK",  states: ["PushBack"] },
  { label: "TAXI OUT",  states: ["TaxiOut"] },
  { label: "FLIGHT",    states: ["Flight"] },
  { label: "TAXI IN",   states: ["TaxiIn"] },
  { label: "ARRIVAL",   states: ["Arrival", "TurnAround"] },
];

function FlightPhaseBox({ phase }: { phase: AutomationState }) {
  return (
    <section className={`${styles.section} ${styles.phaseSection}`}>
      <h3 className={styles.sectionTitle}>Flight Phase</h3>
      <div className={styles.phaseBody}>
        <div className={styles.phaseLabel}>{phase}</div>
        <div className={styles.phaseGrid}>
          {PHASE_BLOCKS.map((b) => (
            <div
              key={b.label}
              className={`${styles.phaseBlock} ${b.states.includes(phase) ? styles.phaseBlockActive : ""}`}
            />
          ))}
          {PHASE_BLOCKS.map((b) => (
            <div key={`${b.label}-l`} className={styles.phaseBlockLabel}>{b.label}</div>
          ))}
        </div>
      </div>
    </section>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className={styles.section}>
      <h3 className={styles.sectionTitle}>{title}</h3>
      <div className={styles.sectionBody}>{children}</div>
    </section>
  );
}

function Indicator({ label, ok, reverseLabel }: { label: string; ok: boolean; reverseLabel?: string }) {
  return (
    <div className={styles.row}>
      <span className={styles.rowLabel}>{label}</span>
      <span className={`${styles.dot} ${ok ? styles.dotOk : styles.dotBad}`} aria-hidden="true" />
      <span className={styles.rowValue}>{ok ? "OK" : reverseLabel ?? "—"}</span>
    </div>
  );
}

function KV({ label, value }: { label: string; value: string }) {
  return (
    <div className={styles.row}>
      <span className={styles.rowLabel}>{label}</span>
      <span className={styles.rowValueMono}>{value}</span>
    </div>
  );
}

function ServiceRow({ label, state }: { label: string; state: GsxServiceState }) {
  const tone = serviceTone(state);
  return (
    <div className={styles.row}>
      <span className={styles.rowLabel}>{label}</span>
      <span className={`${styles.pill} ${styles[`tone_${tone}`]}`}>{state}</span>
    </div>
  );
}

function GpuRow({ connected, relevant }: { connected: boolean; relevant: boolean }) {
  // Grey when phase isn't relevant (Preparation/Departure/Arrival/TurnAround).
  const tone = !relevant ? "neutral" : connected ? "ok" : "bad";
  return (
    <div className={styles.row}>
      <span className={styles.rowLabel}>GPU</span>
      <span className={`${styles.pill} ${styles[`tone_${tone}`]}`}>
        {!relevant ? "—" : connected ? "Connected" : "Disconnected"}
      </span>
    </div>
  );
}

function serviceTone(s: GsxServiceState): "neutral" | "ok" | "warn" | "active" | "bad" {
  switch (s) {
    case "Completed": return "ok";
    case "Active": return "active";
    case "Requested": return "warn";
    case "Callable": return "neutral";
    case "Bypassed":
    case "NotAvailable": return "neutral";
    case "Unknown":
    default: return "bad";
  }
}

function LogTail({ messages }: { messages: string[] }) {
  // Auto-scroll to bottom on new messages, but only when the user hasn't
  // scrolled up to read older entries.
  const ref = useRef<HTMLDivElement>(null);
  const stickRef = useRef(true);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    if (stickRef.current) el.scrollTop = el.scrollHeight;
  }, [messages]);

  function handleScroll() {
    const el = ref.current;
    if (!el) return;
    const threshold = 24;
    stickRef.current = el.scrollTop + el.clientHeight + threshold >= el.scrollHeight;
  }

  return (
    <div className={styles.log} ref={ref} onScroll={handleScroll}>
      {messages.length === 0 ? (
        <div className={styles.logEmpty}>No log entries yet.</div>
      ) : (
        messages.map((m, i) => (
          <div key={i} className={styles.logLine}>{m}</div>
        ))
      )}
    </div>
  );
}
