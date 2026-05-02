import { useEffect, useMemo, useState } from "react";
import { useApi } from "../api/useApi";
import { storeToken } from "../auth/auth";
import { Section } from "../components/forms/Section";
import { BoolField, NumberField, SelectField, TextField } from "../components/forms/Field";
import { PrimaryButton } from "../components/forms/PrimaryButton";
import { DirtyBar } from "../components/forms/DirtyBar";
import {
  AppSettingsDto,
  DISPLAY_UNIT_OPTIONS,
  DISPLAY_UNIT_SOURCE_OPTIONS,
} from "../types";
import styles from "./AppSettingsPanel.module.css";

export function AppSettingsPanel() {
  const { get, post } = useApi();
  // Draft + baseline pattern so we can detect dirty state and so the
  // Discard button reverts to the last server-confirmed snapshot without
  // a network round-trip.
  const [draft, setDraft] = useState<AppSettingsDto | null>(null);
  const [baseline, setBaseline] = useState<AppSettingsDto | null>(null);
  const [themes, setThemes] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);

  async function reload() {
    setError(null);
    try {
      const [dto, t] = await Promise.all([
        get<AppSettingsDto>("/appsettings"),
        get<string[]>("/appsettings/themes"),
      ]);
      setBaseline(dto);
      setDraft(dto);
      setThemes(t);
    } catch (e: unknown) {
      setError((e as Error).message ?? "Failed to load");
    }
  }

  useEffect(() => { reload(); }, []);

  async function save() {
    if (!draft) return;
    setSaving(true);
    setError(null);
    setInfo(null);
    try {
      const fresh = await post<AppSettingsDto>("/appsettings", draft);
      setBaseline(fresh);
      setDraft(fresh);
      setInfo("Saved.");
      setTimeout(() => setInfo(null), 1500);
    } catch (e: unknown) {
      setError((e as Error).message ?? "Save failed");
    } finally {
      setSaving(false);
    }
  }

  function discard() {
    setError(null);
    setInfo(null);
    if (baseline) setDraft(baseline);
  }

  async function regenerateToken() {
    setError(null);
    setInfo(null);
    try {
      const res = await post<{ token: string }>("/appsettings/regenerate-token");
      storeToken(res.token);
      setInfo("New token stored. WebSocket reconnects with the new token.");
      // The server already kicked existing WS connections via TokenRotated;
      // the React WS hook will see close 1008 and bounce to the auth gate
      // unless we proactively put the new token in localStorage first
      // (which we just did) — then reconnect resolves cleanly via the
      // current React session.
      await reload();
    } catch (e: unknown) {
      setError((e as Error).message ?? "Regenerate failed");
    }
  }

  const isDirty = useMemo(() => {
    if (!draft || !baseline) return false;
    if (draft === baseline) return false;
    return JSON.stringify(draft) !== JSON.stringify(baseline);
  }, [draft, baseline]);

  if (!draft) {
    return (
      <div className={styles.loading}>
        {error ? `Error: ${error}` : "Loading settings…"}
      </div>
    );
  }

  function update<K extends keyof AppSettingsDto>(key: K, value: AppSettingsDto[K]) {
    setDraft((d) => (d ? { ...d, [key]: value } : d));
  }

  const themeOptions = themes.map((t) => ({ value: t, label: t }));

  return (
    <div className={styles.panel}>
      <Section title="Theme">
        <SelectField label="Theme" value={draft.currentTheme}
          options={themeOptions.length ? themeOptions : [{ value: draft.currentTheme, label: draft.currentTheme }]}
          onChange={(v) => update("currentTheme", v)} />
      </Section>

      <Section title="Integrations">
        <BoolField label="Use SayIntentions" value={draft.useSayIntentions}
          onChange={(v) => update("useSayIntentions", v)} />
        <BoolField label="Allow Manual Checklist Override" value={draft.allowManualChecklistOverride}
          onChange={(v) => update("allowManualChecklistOverride", v)} />
      </Section>

      <Section title="Display">
        <SelectField label="UI Unit Source" value={draft.displayUnitSource}
          options={DISPLAY_UNIT_SOURCE_OPTIONS}
          onChange={(v) => update("displayUnitSource", v)} />
        <SelectField label="UI Default Unit" value={draft.displayUnitDefault}
          options={DISPLAY_UNIT_OPTIONS}
          onChange={(v) => update("displayUnitDefault", v)} />
        <TextField label="UI Current Unit" value={draft.displayUnitCurrent} readOnly onChange={() => {}} />
        <BoolField label="Open UI on Start" value={draft.openAppWindowOnStart}
          onChange={(v) => update("openAppWindowOnStart", v)} />
        <BoolField label="Solari Animation" value={draft.solariAnimationEnabled}
          onChange={(v) => update("solariAnimationEnabled", v)} />
      </Section>

      <Section title="Fuel & Weight" hint="Values stored in kg; UI shows converted unit">
        <NumberField label="ProSim Bag Weight (kg)" value={draft.prosimWeightBag}
          onChange={(v) => update("prosimWeightBag", v)} />
        <NumberField label="FOB Reset Default (kg)" value={draft.fuelResetDefaultKg}
          onChange={(v) => update("fuelResetDefaultKg", v)} />
        <NumberField label="Fuel Compare Variance (kg)" value={draft.fuelCompareVariance}
          onChange={(v) => update("fuelCompareVariance", v)} />
        <BoolField label="Round Fuel to 100s" value={draft.fuelRoundUp100}
          onChange={(v) => update("fuelRoundUp100", v)} />
      </Section>

      <Section title="Audio Cues">
        <BoolField label="Ding on Startup" value={draft.dingOnStartup}
          onChange={(v) => update("dingOnStartup", v)} />
        <BoolField label="Ding on Final LS" value={draft.dingOnFinal}
          onChange={(v) => update("dingOnFinal", v)} />
      </Section>

      <Section title="Cargo & Doors">
        <NumberField label="Cargo Change Rate" suffix="% / s" value={draft.cargoPercentChangePerSec}
          onChange={(v) => update("cargoPercentChangePerSec", v)} />
        <NumberField label="Cargo Door Open Delay" suffix="s" value={draft.doorCargoOpenDelay}
          onChange={(v) => update("doorCargoOpenDelay", v)} />
        <NumberField label="Cargo Door Close Delay" suffix="s" value={draft.doorCargoDelay}
          onChange={(v) => update("doorCargoDelay", v)} />
      </Section>

      <Section title="GSX Behaviour">
        <BoolField label="Reset GSX Vars in Flight" value={draft.resetGsxStateVarsFlight}
          onChange={(v) => update("resetGsxStateVarsFlight", v)} />
        <BoolField label="Restart GSX on Taxi-In" value={draft.restartGsxOnTaxiIn}
          onChange={(v) => update("restartGsxOnTaxiIn", v)} />
        <BoolField label="Restart GSX on Startup Fail" value={draft.restartGsxStartupFail}
          onChange={(v) => update("restartGsxStartupFail", v)} />
        <NumberField label="Max Startup Failures" value={draft.gsxMenuStartupMaxFail}
          onChange={(v) => update("gsxMenuStartupMaxFail", v)} />
        <BoolField label="Run GSX Service" value={draft.runGsxService}
          onChange={(v) => update("runGsxService", v)} />
        <BoolField label="Run Audio Service" value={draft.runAudioService}
          onChange={(v) => update("runAudioService", v)} />
      </Section>

      <Section title="ProSim SDK">
        <TextField label="ProSim SDK Path" value={draft.proSimSdkPath} monospace
          onChange={(v) => update("proSimSdkPath", v)} />
      </Section>

      <Section title="Web Interface" hint="Hot-toggle on save">
        <BoolField label="Enable Web Server" value={draft.webServerEnabled}
          onChange={(v) => update("webServerEnabled", v)} />
        <NumberField label="Port" value={draft.webServerPort}
          onChange={(v) => update("webServerPort", v)} />
        <BoolField label="Expose to LAN" value={draft.webServerBindAll}
          onChange={(v) => update("webServerBindAll", v)} />
        <TextField label="Auth Token" value={draft.webServerAuthToken} readOnly monospace onChange={() => {}} />
        <div className={styles.tokenActions}>
          <PrimaryButton onClick={regenerateToken} variant="danger">
            Regenerate Token
          </PrimaryButton>
          <span className={styles.hint}>
            Existing clients (including this browser) reconnect with the new token.
          </span>
        </div>
      </Section>

      <DirtyBar
        isDirty={isDirty}
        saving={saving}
        error={error}
        info={info}
        onSave={save}
        onDiscard={discard}
      />
    </div>
  );
}
