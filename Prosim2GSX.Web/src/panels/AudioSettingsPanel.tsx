import { useEffect, useState } from "react";
import { useApi } from "../api/useApi";
import { Section } from "../components/forms/Section";
import { RadioField, SelectField } from "../components/forms/Field";
import { PrimaryButton } from "../components/forms/PrimaryButton";
import {
  ACP_SIDE_OPTIONS,
  AUDIO_CHANNELS,
  AudioChannel,
  AudioDto,
  AudioMappingDto,
  AudioSessionSuggestionDto,
  DATA_FLOW_OPTIONS,
  DEVICE_STATE_OPTIONS,
} from "../types";
import styles from "./AudioSettingsPanel.module.css";

const ELEVATED_SUFFIX = " — elevated";

export function AudioSettingsPanel() {
  const { get, post } = useApi();
  const [draft, setDraft] = useState<AudioDto | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const [suggestions, setSuggestions] = useState<AudioSessionSuggestionDto[]>([]);

  async function reload() {
    setError(null);
    try {
      const dto = await get<AudioDto>("/audio");
      setDraft(dto);
    } catch (e: unknown) {
      setError((e as Error).message ?? "Failed to load");
    }
  }
  async function reloadSuggestions() {
    try {
      const list = await get<AudioSessionSuggestionDto[]>("/audio/process-suggestions");
      setSuggestions(list ?? []);
    } catch { /* leave whatever we had; field still accepts free text */ }
  }
  useEffect(() => {
    reload();
    reloadSuggestions();
  }, []);

  async function save() {
    if (!draft) return;
    setSaving(true); setError(null); setInfo(null);
    try {
      const fresh = await post<AudioDto>("/audio", draft);
      setDraft(fresh);
      setInfo("Saved.");
      setTimeout(() => setInfo(null), 1500);
    } catch (e: unknown) {
      setError((e as Error).message ?? "Save failed");
    } finally {
      setSaving(false);
    }
  }

  if (!draft) {
    return <div className={styles.loading}>{error ? `Error: ${error}` : "Loading audio settings…"}</div>;
  }

  function update<K extends keyof AudioDto>(key: K, value: AudioDto[K]) {
    setDraft((d) => (d ? { ...d, [key]: value } : d));
  }

  function updateMapping(idx: number, partial: Partial<AudioMappingDto>) {
    setDraft((d) => {
      if (!d) return d;
      const mappings = d.mappings.map((m, i) => (i === idx ? { ...m, ...partial } : m));
      return { ...d, mappings };
    });
  }
  function removeMapping(idx: number) {
    setDraft((d) => (d ? { ...d, mappings: d.mappings.filter((_, i) => i !== idx) } : d));
  }
  function addMapping() {
    setDraft((d) => {
      if (!d) return d;
      const fresh: AudioMappingDto = {
        channel: "VHF1",
        device: "",
        binary: "",
        useLatch: true,
        onlyActive: true,
      };
      return { ...d, mappings: [...d.mappings, fresh] };
    });
  }

  function updateBlacklist(idx: number, value: string) {
    setDraft((d) => {
      if (!d) return d;
      const blacklist = d.blacklist.map((s, i) => (i === idx ? value : s));
      return { ...d, blacklist };
    });
  }
  function addBlacklist() {
    setDraft((d) => (d ? { ...d, blacklist: [...d.blacklist, ""] } : d));
  }
  function removeBlacklist(idx: number) {
    setDraft((d) => (d ? { ...d, blacklist: d.blacklist.filter((_, i) => i !== idx) } : d));
  }

  // Per-mapping elevated-status derived from suggestions: matching binary
  // entry that is currently NOT accessible. Mirrors the WPF Status column /
  // banner without needing a transient field on the DTO.
  const suggestionByName = new Map(
    suggestions.map((s) => [s.processName.toLowerCase(), s]),
  );
  const elevatedBinaries = Array.from(new Set(
    draft.mappings
      .map((m) => m.binary)
      .filter((b) => {
        const match = suggestionByName.get((b ?? "").toLowerCase());
        return match != null && !match.isAccessible;
      }),
  ));

  return (
    <div className={styles.panel}>
      <div className={styles.toolbar}>
        <PrimaryButton onClick={save} disabled={saving}>Save</PrimaryButton>
        <PrimaryButton onClick={reload} variant="secondary" disabled={saving}>Reload</PrimaryButton>
        <div className={styles.toolbarStatus}>
          {error && <span className={styles.error}>{error}</span>}
          {info && <span className={styles.info}>{info}</span>}
        </div>
      </div>

      <Section title="Audio API">
        <RadioField
          label="Backend"
          name="audioApi"
          value={draft.isCoreAudioSelected ? "core" : "voicemeeter"}
          options={[
            { value: "core", label: "Core Audio (Process Control)" },
            { value: "voicemeeter", label: "VoiceMeeter API (Strip Control)" },
          ]}
          onChange={(v) => update("isCoreAudioSelected", v === "core")}
        />
        <SelectField label="ACP Side" value={draft.audioAcpSide}
          options={ACP_SIDE_OPTIONS}
          onChange={(v) => update("audioAcpSide", v)} />
        <SelectField label="Device Flow" value={draft.audioDeviceFlow}
          options={DATA_FLOW_OPTIONS}
          onChange={(v) => update("audioDeviceFlow", v)} />
        <SelectField label="Device State" value={draft.audioDeviceState}
          options={DEVICE_STATE_OPTIONS}
          onChange={(v) => update("audioDeviceState", v)} />
      </Section>

      <Section title="App → Channel Mappings">
        {elevatedBinaries.length > 0 && (
          <div className={styles.warningBanner}>
            Elevated process(es) detected: {elevatedBinaries.join(", ")}.
            Run Prosim2GSX as administrator to control these apps —
            otherwise these mappings are inactive.
          </div>
        )}

        <div className={styles.mappingsHeader}>
          <span>Channel</span>
          <span>Binary</span>
          <span>Device</span>
          <span>Latch</span>
          <span>Active</span>
          <span>Status</span>
          <span />
        </div>
        {draft.mappings.length === 0 && <div className={styles.empty}>No mappings configured.</div>}
        {draft.mappings.map((m, i) => {
          const match = suggestionByName.get((m.binary ?? "").toLowerCase());
          const isElevated = match != null && !match.isAccessible;
          return (
            <div key={i} className={styles.mappingRow}>
              <select value={m.channel}
                onChange={(e) => updateMapping(i, { channel: e.target.value as AudioChannel })}
                className={styles.cellSelect}>
                {AUDIO_CHANNELS.map((c) => <option key={c} value={c}>{c}</option>)}
              </select>
              <input value={m.binary}
                list="audio-process-suggestions"
                onFocus={reloadSuggestions}
                onChange={(e) => {
                  let v = e.target.value;
                  if (v.endsWith(ELEVATED_SUFFIX)) v = v.slice(0, -ELEVATED_SUFFIX.length);
                  updateMapping(i, { binary: v });
                }}
                placeholder="ProcessName"
                className={styles.cellInput} />
              <input value={m.device}
                onChange={(e) => updateMapping(i, { device: e.target.value })}
                placeholder="(All)"
                className={styles.cellInput} />
              <input type="checkbox" checked={m.useLatch}
                onChange={(e) => updateMapping(i, { useLatch: e.target.checked })} />
              <input type="checkbox" checked={m.onlyActive}
                onChange={(e) => updateMapping(i, { onlyActive: e.target.checked })} />
              <span className={styles.statusCell} title={isElevated ? "Elevated — run Prosim2GSX as admin" : ""}>
                {isElevated ? "Elevated — run Prosim2GSX as admin" : ""}
              </span>
              <button type="button" className={styles.removeBtn} onClick={() => removeMapping(i)}>×</button>
            </div>
          );
        })}
        <PrimaryButton onClick={addMapping} variant="secondary">Add mapping</PrimaryButton>
      </Section>

      <datalist id="audio-process-suggestions">
        {suggestions.map((s) => (
          <option key={s.processName} value={s.isAccessible ? s.processName : s.processName + ELEVATED_SUFFIX} />
        ))}
      </datalist>

      <Section title="Device Blacklist">
        {draft.blacklist.length === 0 && <div className={styles.empty}>No devices blacklisted.</div>}
        {draft.blacklist.map((d, i) => (
          <div key={i} className={styles.blacklistRow}>
            <input value={d}
              onChange={(e) => updateBlacklist(i, e.target.value)}
              placeholder="Device name"
              className={styles.cellInput} />
            <button type="button" className={styles.removeBtn} onClick={() => removeBlacklist(i)}>×</button>
          </div>
        ))}
        <PrimaryButton onClick={addBlacklist} variant="secondary">Add device</PrimaryButton>
      </Section>
    </div>
  );
}
