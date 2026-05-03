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
  VoiceMeeterMappingDto,
  VoiceMeeterStripDto,
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
  const [strips, setStrips] = useState<VoiceMeeterStripDto[]>([]);

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
  async function reloadStrips() {
    try {
      const list = await get<VoiceMeeterStripDto[]>("/voicemeeter/strips");
      setStrips(list ?? []);
    } catch { setStrips([]); }
  }
  useEffect(() => {
    reload();
    reloadSuggestions();
    reloadStrips();
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

  function updateVmMapping(idx: number, partial: Partial<VoiceMeeterMappingDto>) {
    setDraft((d) => {
      if (!d) return d;
      const list = d.voiceMeeterMappings.map((m, i) => (i === idx ? { ...m, ...partial } : m));
      return { ...d, voiceMeeterMappings: list };
    });
  }
  function removeVmMapping(idx: number) {
    setDraft((d) => (d ? { ...d, voiceMeeterMappings: d.voiceMeeterMappings.filter((_, i) => i !== idx) } : d));
  }
  function addVmMapping() {
    setDraft((d) => {
      if (!d) return d;
      const fresh: VoiceMeeterMappingDto = {
        channel: "VHF1",
        stripIndex: 0,
        isBus: false,
        useLatch: true,
      };
      return { ...d, voiceMeeterMappings: [...d.voiceMeeterMappings, fresh] };
    });
  }
  function setVmMappingTargetFromKey(idx: number, key: string) {
    if (!key) return;
    const colon = key.indexOf(":");
    if (colon <= 0) return;
    const isBus = key.slice(0, colon) === "bus";
    const stripIdx = parseInt(key.slice(colon + 1), 10);
    if (Number.isNaN(stripIdx)) return;
    updateVmMapping(idx, { stripIndex: stripIdx, isBus });
  }
  function vmMappingKey(m: VoiceMeeterMappingDto) {
    return `${m.isBus ? "bus" : "strip"}:${m.stripIndex}`;
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

  const vmEnabled = draft.useVoiceMeeter;
  const vmAvailable = strips.length > 0;
  const vmWarning = !vmEnabled
    ? ""
    : !draft.voiceMeeterDllPath
      ? "VoiceMeeter integration is enabled but no DLL path is configured. Set the path below."
      : !vmAvailable
        ? "VoiceMeeter is not running or the Remote API DLL was not found."
        : "";

  function setBackend(useVm: boolean) {
    setDraft((d) => (d ? { ...d, useVoiceMeeter: useVm, isCoreAudioSelected: !useVm } : d));
    if (useVm) reloadStrips();
  }

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
          value={draft.useVoiceMeeter ? "voicemeeter" : "core"}
          options={[
            { value: "core", label: "Core Audio (Process Control)" },
            { value: "voicemeeter", label: "VoiceMeeter API (Strip Control)" },
          ]}
          onChange={(v) => setBackend(v === "voicemeeter")}
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

      <Section title="VoiceMeeter">
        {vmWarning && <div className={styles.warningBanner}>{vmWarning}</div>}
        <div className={styles.vmPathRow}>
          <label className={styles.vmPathLabel}>Remote DLL path</label>
          <input
            type="text"
            value={draft.voiceMeeterDllPath}
            placeholder="C:\Program Files (x86)\VB\Voicemeeter\VoicemeeterRemote64.dll"
            onChange={(e) => update("voiceMeeterDllPath", e.target.value)}
            className={styles.cellInput}
            spellCheck={false}
          />
          <button type="button" className={styles.vmReloadBtn} onClick={reloadStrips}>
            Reload strips
          </button>
        </div>
      </Section>

      {!vmEnabled && (
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
      )}

      {vmEnabled && (
        <Section title="VoiceMeeter Mappings">
          <div className={styles.vmMappingsHeader}>
            <span>Channel</span>
            <span>Strip / Bus</span>
            <span>Latch</span>
            <span />
          </div>
          {draft.voiceMeeterMappings.length === 0 && <div className={styles.empty}>No VoiceMeeter mappings configured.</div>}
          {draft.voiceMeeterMappings.map((m, i) => (
            <div key={i} className={styles.vmMappingsRow}>
              <select value={m.channel}
                onChange={(e) => updateVmMapping(i, { channel: e.target.value as AudioChannel })}
                className={styles.cellSelect}>
                {AUDIO_CHANNELS.map((c) => <option key={c} value={c}>{c}</option>)}
              </select>
              <select value={vmMappingKey(m)}
                onChange={(e) => setVmMappingTargetFromKey(i, e.target.value)}
                disabled={strips.length === 0}
                className={styles.cellSelect}>
                {strips.length === 0 && <option value="">(no strips available — load DLL or start VoiceMeeter)</option>}
                {strips.map((s) => (
                  <option key={s.key} value={s.key}>{s.displayName}</option>
                ))}
              </select>
              <input type="checkbox" checked={m.useLatch}
                onChange={(e) => updateVmMapping(i, { useLatch: e.target.checked })} />
              <button type="button" className={styles.removeBtn} onClick={() => removeVmMapping(i)}>×</button>
            </div>
          ))}
          <PrimaryButton onClick={addVmMapping} variant="secondary">Add VoiceMeeter mapping</PrimaryButton>
        </Section>
      )}

      <datalist id="audio-process-suggestions">
        {suggestions.map((s) => (
          <option key={s.processName} value={s.isAccessible ? s.processName : s.processName + ELEVATED_SUFFIX} />
        ))}
      </datalist>

      {!vmEnabled && (
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
      )}
    </div>
  );
}
