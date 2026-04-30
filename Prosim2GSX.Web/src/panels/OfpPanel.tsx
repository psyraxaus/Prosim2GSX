import { FormEvent, useEffect, useState } from "react";
import { useApi } from "../api/useApi";
import { useAppState } from "../state/AppStateContext";
import { Section } from "../components/forms/Section";
import { PrimaryButton } from "../components/forms/PrimaryButton";
import { KorryPushbackButton } from "../components/KorryPushbackButton";
import {
  ConfirmArrivalGateRequest,
  OfpDto,
  PushbackPreference,
  SetPushbackPreferenceRequest,
  WeatherDto,
} from "../types";
import styles from "./OfpPanel.module.css";

// Full OFP tab. Mirrors the WPF ModelOfp surface: read-only OFP summary,
// arrival-gate input + Confirm + Send Now, pushback preference, weather
// cards. Auto-refreshes weather on tab activation per the locked decision
// in project memory phase8_design_decisions.
//
// Live updates flow in via the WS "ofp" channel (subscribed by
// StateWebSocketHandler — broadcasts OfpState property changes and
// GsxController.PushbackPreferenceChanged events). The panel REST-fetches
// once on mount; subsequent updates merge into AppStateContext.ofp.
export function OfpPanel() {
  const { get, post } = useApi();
  const { state, dispatch } = useAppState();

  const [arrivalGateInput, setArrivalGateInput] = useState("");
  const [busy, setBusy] = useState<"confirm" | "sendNow" | "weather" | "pushback" | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function loadOfp() {
    setError(null);
    try {
      const dto = await get<OfpDto>("/ofp");
      dispatch({ type: "set", channel: "ofp", state: dto as unknown as Record<string, unknown> });
    } catch (e: unknown) {
      setError((e as Error).message ?? "Failed to load");
    }
  }

  async function refreshWeather() {
    setBusy("weather");
    setError(null);
    try {
      // Server updates OfpState; WS pushes the patch. Discard the response —
      // we read from state.ofp.
      await post("/ofp/refresh-weather");
    } catch (e: unknown) {
      setError((e as Error).message ?? "Weather refresh failed");
    } finally {
      setBusy(null);
    }
  }

  useEffect(() => {
    // Both fire on mount; mount = tab activation because App.tsx
    // conditionally renders this component when active === "ofp".
    loadOfp();
    refreshWeather();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const ofp = state.ofp as unknown as OfpDto | null;
  if (!ofp) {
    return (
      <div className={styles.loading}>
        {error ? `Error: ${error}` : "Loading OFP…"}
      </div>
    );
  }

  async function confirmGate(e: FormEvent) {
    e.preventDefault();
    const gate = arrivalGateInput.trim().toUpperCase();
    if (!gate) return;
    setBusy("confirm");
    setError(null);
    try {
      const req: ConfirmArrivalGateRequest = { gate };
      await post("/ofp/confirm-arrival-gate", req);
      setArrivalGateInput("");
    } catch (e: unknown) {
      setError((e as Error).message ?? "Confirm failed");
    } finally {
      setBusy(null);
    }
  }

  async function sendNow() {
    setBusy("sendNow");
    setError(null);
    try {
      await post("/ofp/send-now");
    } catch (e: unknown) {
      setError((e as Error).message ?? "Send Now failed");
    } finally {
      setBusy(null);
    }
  }

  async function setPushback(preference: PushbackPreference) {
    // Narrowing of `ofp` from the early-return check above is lost inside
    // this nested closure under TS strict mode, so re-guard here.
    if (!ofp || preference === ofp.pushbackPreference) return;
    setBusy("pushback");
    setError(null);
    try {
      const req: SetPushbackPreferenceRequest = { preference };
      await post("/ofp/set-pushback-preference", req);
    } catch (e: unknown) {
      setError((e as Error).message ?? "Pushback set failed");
    } finally {
      setBusy(null);
    }
  }

  return (
    <div className={styles.panel}>
      {error && <div className={styles.error}>{error}</div>}

      <Section title="Flight Information">
        {ofp.isOfpLoaded ? (
          <div className={styles.kvGrid}>
            <KV label="Departure" value={ofp.departureIcao} mono />
            <KV label="Arrival" value={ofp.arrivalIcao} mono />
            <KV label="Alternate" value={ofp.alternateIcao || "—"} mono />
            <KV label="Flight" value={ofp.flightNumber || "—"} mono />
            <KV label="Plan RWY Out" value={ofp.departurePlanRwy || "—"} mono />
            <KV label="Plan RWY In" value={ofp.arrivalPlanRwy || "—"} mono />
            <KV label="Cruise Alt" value={ofp.cruiseAltitude || "—"} mono />
            <KV label="Block Fuel" value={ofp.blockFuelKg || "—"} mono />
            <KV label="Block Time" value={ofp.blockTimeFormatted || "—"} mono />
            <KV label="Pax" value={ofp.paxCount || "—"} mono />
            <KV label="Air Distance" value={ofp.airDistance || "—"} mono />
          </div>
        ) : (
          <div className={styles.empty}>No OFP loaded. Import a flight plan in ProSim.</div>
        )}
      </Section>

      <Section title="Pushback Direction">
        <div className={styles.korryGroup} role="group" aria-label="Pushback direction">
          <KorryPushbackButton
            arrow="tailLeft"
            label="TAIL LEFT"
            subtitle="NOSE RIGHT"
            isActive={ofp.pushbackPreference === "TailLeft"}
            onClick={() => setPushback("TailLeft")}
            disabled={busy === "pushback"}
            title="Auto-select 'Nose Right / Tail Left' when GSX shows the direction menu."
          />
          <KorryPushbackButton
            arrow="straight"
            label="STRAIGHT"
            subtitle="STRAIGHT BACK"
            isActive={ofp.pushbackPreference === "Straight"}
            onClick={() => setPushback("Straight")}
            disabled={busy === "pushback"}
            title="Auto-select 'Straight pushback' when GSX shows the direction menu."
          />
          <KorryPushbackButton
            arrow="tailRight"
            label="TAIL RIGHT"
            subtitle="NOSE LEFT"
            isActive={ofp.pushbackPreference === "TailRight"}
            onClick={() => setPushback("TailRight")}
            disabled={busy === "pushback"}
            title="Auto-select 'Nose Left / Tail Right' when GSX shows the direction menu."
          />
        </div>
      </Section>

      <Section title="Arrival Gate Assignment">
        <form className={styles.gateForm} onSubmit={confirmGate}>
          <input
            type="text"
            value={arrivalGateInput}
            onChange={(e) => setArrivalGateInput(e.target.value.toUpperCase())}
            placeholder="GATE"
            disabled={!ofp.isOfpLoaded || busy === "confirm"}
            className={styles.gateInput}
            autoCapitalize="characters"
            spellCheck={false}
          />
          <PrimaryButton
            onClick={() => {}}
            disabled={!ofp.isOfpLoaded || !arrivalGateInput.trim() || busy === "confirm"}
          >
            {busy === "confirm" ? "Confirming…" : "Confirm"}
          </PrimaryButton>
          <PrimaryButton
            variant="secondary"
            onClick={sendNow}
            disabled={!ofp.pendingArrivalGate || busy === "sendNow"}
          >
            {busy === "sendNow" ? "Sending…" : "Send Now"}
          </PrimaryButton>
        </form>

        {ofp.pendingArrivalGate && (
          <div className={styles.pending}>
            <span className={styles.pendingLabel}>Pending</span>
            <span className={styles.pendingValue}>{ofp.pendingArrivalGate}</span>
          </div>
        )}

        {ofp.assignedArrivalGate && (
          <div className={styles.assigned}>
            <span className={styles.assignedLabel}>GSX Assigned</span>
            <span className={styles.assignedValue}>{ofp.assignedArrivalGate}</span>
          </div>
        )}

        {ofp.gateAssignmentStatus && (
          <StatusLine label="ATC" value={ofp.gateAssignmentStatus} />
        )}
        {ofp.gsxAssignmentStatus && (
          <StatusLine label="GSX" value={ofp.gsxAssignmentStatus} />
        )}
      </Section>

      <Section title="Weather" hint={ofp.sayIntentionsActive ? "" : "SayIntentions inactive"}>
        <div className={styles.weatherToolbar}>
          <PrimaryButton
            variant="secondary"
            onClick={refreshWeather}
            disabled={busy === "weather" || !ofp.sayIntentionsActive}
          >
            {busy === "weather" || ofp.isRefreshingWeather ? "Refreshing…" : "Refresh Weather"}
          </PrimaryButton>
          {ofp.weatherStatus && <span className={styles.weatherStatus}>{ofp.weatherStatus}</span>}
        </div>

        <div className={styles.weatherGrid}>
          <WeatherCard title={`Departure ${ofp.departureIcao || ""}`} wx={ofp.departureWeather} />
          <WeatherCard title={`Arrival ${ofp.arrivalIcao || ""}`} wx={ofp.arrivalWeather} />
        </div>
      </Section>
    </div>
  );
}

function KV({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className={styles.kv}>
      <span className={styles.kvLabel}>{label}</span>
      <span className={mono ? styles.kvValueMono : styles.kvValue}>{value}</span>
    </div>
  );
}

function StatusLine({ label, value }: { label: string; value: string }) {
  return (
    <div className={styles.statusLine}>
      <span className={styles.statusLabel}>{label}</span>
      <span className={styles.statusValue}>{value}</span>
    </div>
  );
}

function WeatherCard({ title, wx }: { title: string; wx: WeatherDto | null }) {
  if (!wx) {
    return (
      <div className={styles.weatherCard}>
        <div className={styles.weatherTitle}>{title}</div>
        <div className={styles.empty}>No weather data.</div>
      </div>
    );
  }
  return (
    <div className={styles.weatherCard}>
      <div className={styles.weatherTitle}>{title}</div>
      {wx.activeRunway && (
        <div className={styles.weatherRow}>
          <span className={styles.weatherLabel}>Active RWY</span>
          <span className={styles.weatherValue}>{wx.activeRunway}</span>
        </div>
      )}
      {(wx.windDirection !== null || wx.windSpeed !== null) && (
        <div className={styles.weatherRow}>
          <span className={styles.weatherLabel}>Wind</span>
          <span className={styles.weatherValue}>
            {wx.windDirection !== null ? `${wx.windDirection}°` : "—"} / {wx.windSpeed !== null ? `${wx.windSpeed}kt` : "—"}
          </span>
        </div>
      )}
      {wx.metar && (
        <div className={styles.weatherBlock}>
          <div className={styles.weatherLabel}>METAR</div>
          <div className={styles.weatherValueBlock}>{wx.metar}</div>
        </div>
      )}
      {wx.atis && (
        <div className={styles.weatherBlock}>
          <div className={styles.weatherLabel}>ATIS</div>
          <div className={styles.weatherValueBlock}>{wx.atis}</div>
        </div>
      )}
      {wx.taf && (
        <div className={styles.weatherBlock}>
          <div className={styles.weatherLabel}>TAF</div>
          <div className={styles.weatherValueBlock}>{wx.taf}</div>
        </div>
      )}
    </div>
  );
}
