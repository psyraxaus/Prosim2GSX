import { useEffect, useState } from "react";
import { useApi } from "../api/useApi";
import { Section } from "../components/forms/Section";
import { BoolField, NumberField, SelectField } from "../components/forms/Field";
import { PrimaryButton } from "../components/forms/PrimaryButton";
import {
  AUTO_DEICE_FLUID_OPTIONS,
  CONNECT_PCA_OPTIONS,
  GsxSettingsDto,
  PUSHBACK_TIMING_OPTIONS,
  REFUEL_METHOD_OPTIONS,
  REMOVE_STAIRS_OPTIONS,
  SERVICE_ACTIVATION_OPTIONS,
  SERVICE_CONSTRAINT_OPTIONS,
  SERVICE_TYPE_OPTIONS,
  ServiceConfigDto,
  TUG_OPTIONS,
} from "../types";
import styles from "./GsxSettingsPanel.module.css";

export function GsxSettingsPanel() {
  const { get, post } = useApi();
  const [draft, setDraft] = useState<GsxSettingsDto | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);

  async function reload() {
    setError(null);
    try {
      const dto = await get<GsxSettingsDto>("/gsxsettings");
      setDraft(dto);
    } catch (e: unknown) {
      setError((e as Error).message ?? "Failed to load");
    }
  }
  useEffect(() => { reload(); }, []);

  async function save() {
    if (!draft) return;
    setSaving(true); setError(null); setInfo(null);
    try {
      const fresh = await post<GsxSettingsDto>("/gsxsettings", draft);
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
    return <div className={styles.loading}>{error ? `Error: ${error}` : "Loading GSX settings…"}</div>;
  }

  function update<K extends keyof GsxSettingsDto>(key: K, value: GsxSettingsDto[K]) {
    setDraft((d) => (d ? { ...d, [key]: value } : d));
  }

  function updateService(idx: number, partial: Partial<ServiceConfigDto>) {
    setDraft((d) => {
      if (!d) return d;
      const departureServices = d.departureServices.map((s, i) => (i === idx ? { ...s, ...partial } : s));
      return { ...d, departureServices };
    });
  }

  function moveService(idx: number, dir: -1 | 1) {
    setDraft((d) => {
      if (!d) return d;
      const next = idx + dir;
      if (next < 0 || next >= d.departureServices.length) return d;
      const services = [...d.departureServices];
      [services[idx], services[next]] = [services[next], services[idx]];
      return { ...d, departureServices: services };
    });
  }

  function removeService(idx: number) {
    setDraft((d) => (d ? { ...d, departureServices: d.departureServices.filter((_, i) => i !== idx) } : d));
  }

  function addService() {
    setDraft((d) => {
      if (!d) return d;
      const fresh: ServiceConfigDto = {
        serviceType: "Unknown",
        serviceActivation: "Manual",
        serviceConstraint: "NoneAlways",
        minimumFlightDuration: 0,
      };
      return { ...d, departureServices: [...d.departureServices, fresh] };
    });
  }

  function updateListItem(field: "operatorPreferences" | "companyHubs", idx: number, value: string) {
    setDraft((d) => {
      if (!d) return d;
      const list = d[field].map((s, i) => (i === idx ? value : s));
      return { ...d, [field]: list };
    });
  }
  function addListItem(field: "operatorPreferences" | "companyHubs") {
    setDraft((d) => (d ? { ...d, [field]: [...d[field], ""] } : d));
  }
  function removeListItem(field: "operatorPreferences" | "companyHubs", idx: number) {
    setDraft((d) => (d ? { ...d, [field]: d[field].filter((_, i) => i !== idx) } : d));
  }

  return (
    <div className={styles.panel}>
      <div className={styles.toolbar}>
        <PrimaryButton onClick={save} disabled={saving}>Save</PrimaryButton>
        <PrimaryButton onClick={reload} variant="secondary" disabled={saving}>Reload</PrimaryButton>
        <div className={styles.toolbarStatus}>
          {draft.profileName && <span className={styles.profileName}>Profile: {draft.profileName}</span>}
          {error && <span className={styles.error}>{error}</span>}
          {info && <span className={styles.info}>{info}</span>}
        </div>
      </div>

      <Section title="Doors & Stairs">
        <BoolField label="Door Stair Handling" value={draft.doorStairHandling}
          onChange={(v) => update("doorStairHandling", v)} />
        <BoolField label="Include L2 Door" value={draft.doorStairIncludeL2}
          onChange={(v) => update("doorStairIncludeL2", v)} />
        <BoolField label="Door Cargo Handling" value={draft.doorCargoHandling}
          onChange={(v) => update("doorCargoHandling", v)} />
        <BoolField label="Door Catering Handling" value={draft.doorCateringHandling}
          onChange={(v) => update("doorCateringHandling", v)} />
        <BoolField label="Door Open on Boarding Active" value={draft.doorOpenBoardActive}
          onChange={(v) => update("doorOpenBoardActive", v)} />
        <BoolField label="Cargo Doors Keep Open on Loaded" value={draft.doorsCargoKeepOpenOnLoaded}
          onChange={(v) => update("doorsCargoKeepOpenOnLoaded", v)} />
        <BoolField label="Cargo Doors Keep Open on Unloaded" value={draft.doorsCargoKeepOpenOnUnloaded}
          onChange={(v) => update("doorsCargoKeepOpenOnUnloaded", v)} />
        <BoolField label="Close Doors on Final" value={draft.closeDoorsOnFinal}
          onChange={(v) => update("closeDoorsOnFinal", v)} />
      </Section>

      <Section title="Jetway / Stairs">
        <BoolField label="Call on Preparation" value={draft.callJetwayStairsOnPrep}
          onChange={(v) => update("callJetwayStairsOnPrep", v)} />
        <BoolField label="Call During Departure" value={draft.callJetwayStairsDuringDeparture}
          onChange={(v) => update("callJetwayStairsDuringDeparture", v)} />
        <BoolField label="Call on Arrival" value={draft.callJetwayStairsOnArrival}
          onChange={(v) => update("callJetwayStairsOnArrival", v)} />
        <SelectField label="Remove Stairs After Departure" value={draft.removeStairsAfterDepature}
          options={REMOVE_STAIRS_OPTIONS}
          onChange={(v) => update("removeStairsAfterDepature", v)} />
        <BoolField label="Remove Jetway/Stairs on Final" value={draft.removeJetwayStairsOnFinal}
          onChange={(v) => update("removeJetwayStairsOnFinal", v)} />
      </Section>

      <Section title="Ground Equipment">
        <BoolField label="Place ProSim Stairs on Walkaround" value={draft.placeProsimStairsWalkaround}
          onChange={(v) => update("placeProsimStairsWalkaround", v)} />
        <BoolField label="Clear Ground Equip on Beacon" value={draft.clearGroundEquipOnBeacon}
          onChange={(v) => update("clearGroundEquipOnBeacon", v)} />
        <BoolField label="Gradual Ground Equip Removal" value={draft.gradualGroundEquipRemoval}
          onChange={(v) => update("gradualGroundEquipRemoval", v)} />
        <BoolField label="Connect GPU with APU Running" value={draft.connectGpuWithApuRunning}
          onChange={(v) => update("connectGpuWithApuRunning", v)} />
        <SelectField label="Connect PCA" value={draft.connectPca}
          options={CONNECT_PCA_OPTIONS}
          onChange={(v) => update("connectPca", v)} />
        <BoolField label="PCA Override" value={draft.pcaOverride}
          onChange={(v) => update("pcaOverride", v)} />
        <NumberField label="Chock Delay Min (s)" value={draft.chockDelayMin}
          onChange={(v) => update("chockDelayMin", v)} />
        <NumberField label="Chock Delay Max (s)" value={draft.chockDelayMax}
          onChange={(v) => update("chockDelayMax", v)} />
      </Section>

      <Section title="Services">
        <BoolField label="Call Reposition" value={draft.callReposition}
          onChange={(v) => update("callReposition", v)} />
        <BoolField label="Call Deboard on Arrival" value={draft.callDeboardOnArrival}
          onChange={(v) => update("callDeboardOnArrival", v)} />
        <BoolField label="Run Departure During Deboarding" value={draft.runDepartureDuringDeboarding}
          onChange={(v) => update("runDepartureDuringDeboarding", v)} />
        <BoolField label="Chime on Parked" value={draft.chimeOnParked}
          onChange={(v) => update("chimeOnParked", v)} />
        <BoolField label="Chime on Deboard Complete" value={draft.chimeOnDeboardComplete}
          onChange={(v) => update("chimeOnDeboardComplete", v)} />
      </Section>

      <Section title="Departure Service Order" hint="Order = activation sequence">
        <div className={styles.servicesHeader}>
          <span>Service</span>
          <span>Activation</span>
          <span>Constraint</span>
          <span>Min Flight</span>
          <span />
        </div>
        {draft.departureServices.length === 0 && (
          <div className={styles.empty}>No departure services configured.</div>
        )}
        {draft.departureServices.map((s, idx) => (
          <div key={idx} className={styles.serviceRow}>
            <select value={s.serviceType}
              onChange={(e) => updateService(idx, { serviceType: e.target.value as typeof s.serviceType })}
              className={styles.cellSelect}>
              {SERVICE_TYPE_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
            </select>
            <select value={s.serviceActivation}
              onChange={(e) => updateService(idx, { serviceActivation: e.target.value as typeof s.serviceActivation })}
              className={styles.cellSelect}>
              {SERVICE_ACTIVATION_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
            </select>
            <select value={s.serviceConstraint}
              onChange={(e) => updateService(idx, { serviceConstraint: e.target.value as typeof s.serviceConstraint })}
              className={styles.cellSelect}>
              {SERVICE_CONSTRAINT_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
            </select>
            <input type="number" value={s.minimumFlightDuration}
              onChange={(e) => updateService(idx, { minimumFlightDuration: Number(e.target.value) })}
              className={styles.cellNumber}
              title="Minimum flight duration (seconds)" />
            <div className={styles.serviceActions}>
              <button type="button" className={styles.iconBtn} onClick={() => moveService(idx, -1)} disabled={idx === 0} title="Move up">▲</button>
              <button type="button" className={styles.iconBtn} onClick={() => moveService(idx, 1)} disabled={idx === draft.departureServices.length - 1} title="Move down">▼</button>
              <button type="button" className={styles.removeBtn} onClick={() => removeService(idx)} title="Remove">×</button>
            </div>
          </div>
        ))}
        <PrimaryButton onClick={addService} variant="secondary">Add service</PrimaryButton>
      </Section>

      <Section title="Refuel">
        <SelectField label="Refuel Method" value={draft.refuelMethod}
          options={REFUEL_METHOD_OPTIONS}
          onChange={(v) => update("refuelMethod", v)} />
        <NumberField label="Refuel Rate (kg/s)" value={draft.refuelRateKgSec} step={0.5}
          onChange={(v) => update("refuelRateKgSec", v)} />
        <NumberField label="Target Refuel Time (s)" value={draft.refuelTimeTargetSeconds}
          onChange={(v) => update("refuelTimeTargetSeconds", v)} />
        <BoolField label="Skip Fuel on Tankering" value={draft.skipFuelOnTankering}
          onChange={(v) => update("skipFuelOnTankering", v)} />
        <BoolField label="Refuel Finish on Hose" value={draft.refuelFinishOnHose}
          onChange={(v) => update("refuelFinishOnHose", v)} />
      </Section>

      <Section title="Pushback">
        <SelectField label="Attach Tug During Boarding" value={draft.attachTugDuringBoarding}
          options={TUG_OPTIONS}
          onChange={(v) => update("attachTugDuringBoarding", v)} />
        <SelectField label="Call Pushback When Tug Attached" value={draft.callPushbackWhenTugAttached}
          options={PUSHBACK_TIMING_OPTIONS}
          onChange={(v) => update("callPushbackWhenTugAttached", v)} />
        <BoolField label="Call Pushback on Beacon" value={draft.callPushbackOnBeacon}
          onChange={(v) => update("callPushbackOnBeacon", v)} />
      </Section>

      <Section title="Beacon-Orchestrated Sequence">
        <BoolField label="Sequence on Beacon" value={draft.sequenceOnBeacon}
          onChange={(v) => update("sequenceOnBeacon", v)} />
        <NumberField label="Doors Close Delay Min (s)" value={draft.seqDoorsCloseDelayMin}
          onChange={(v) => update("seqDoorsCloseDelayMin", v)} />
        <NumberField label="Doors Close Delay Max (s)" value={draft.seqDoorsCloseDelayMax}
          onChange={(v) => update("seqDoorsCloseDelayMax", v)} />
        <NumberField label="Jetway Retract Delay Min (s)" value={draft.seqJetwayRetractDelayMin}
          onChange={(v) => update("seqJetwayRetractDelayMin", v)} />
        <NumberField label="Jetway Retract Delay Max (s)" value={draft.seqJetwayRetractDelayMax}
          onChange={(v) => update("seqJetwayRetractDelayMax", v)} />
        <NumberField label="GPU Disconnect Delay Min (s)" value={draft.seqGpuDisconnectDelayMin}
          onChange={(v) => update("seqGpuDisconnectDelayMin", v)} />
        <NumberField label="GPU Disconnect Delay Max (s)" value={draft.seqGpuDisconnectDelayMax}
          onChange={(v) => update("seqGpuDisconnectDelayMax", v)} />
      </Section>

      <Section title="Operator">
        <BoolField label="Operator Auto-Select" value={draft.operatorAutoSelect}
          onChange={(v) => update("operatorAutoSelect", v)} />
        <ListEditor label="Operator Preferences"
          values={draft.operatorPreferences}
          onChangeAt={(i, v) => updateListItem("operatorPreferences", i, v)}
          onAdd={() => addListItem("operatorPreferences")}
          onRemove={(i) => removeListItem("operatorPreferences", i)} />
      </Section>

      <Section title="Company Hubs">
        <ListEditor label="ICAO Hubs"
          values={draft.companyHubs}
          onChangeAt={(i, v) => updateListItem("companyHubs", i, v)}
          onAdd={() => addListItem("companyHubs")}
          onRemove={(i) => removeListItem("companyHubs", i)} />
      </Section>

      <Section title="Skip Questions">
        <BoolField label="Skip Walkaround" value={draft.skipWalkAround}
          onChange={(v) => update("skipWalkAround", v)} />
        <BoolField label="Skip Crew Question" value={draft.skipCrewQuestion}
          onChange={(v) => update("skipCrewQuestion", v)} />
        <BoolField label="Skip Follow Me" value={draft.skipFollowMe}
          onChange={(v) => update("skipFollowMe", v)} />
        <BoolField label="Keep Direction Menu Open" value={draft.keepDirectionMenuOpen}
          onChange={(v) => update("keepDirectionMenuOpen", v)} />
        <BoolField label="Answer Cabin Call (Ground)" value={draft.answerCabinCallGround}
          onChange={(v) => update("answerCabinCallGround", v)} />
        <NumberField label="Cabin Call Delay (Ground, ms)" value={draft.delayCabinCallGround}
          onChange={(v) => update("delayCabinCallGround", v)} />
        <BoolField label="Answer Cabin Call (Air)" value={draft.answerCabinCallAir}
          onChange={(v) => update("answerCabinCallAir", v)} />
        <NumberField label="Cabin Call Delay (Air, ms)" value={draft.delayCabinCallAir}
          onChange={(v) => update("delayCabinCallAir", v)} />
      </Section>

      <Section title="Aircraft / OFP">
        <NumberField label="Final Delay Min (s)" value={draft.finalDelayMin}
          onChange={(v) => update("finalDelayMin", v)} />
        <NumberField label="Final Delay Max (s)" value={draft.finalDelayMax}
          onChange={(v) => update("finalDelayMax", v)} />
        <BoolField label="Save / Load FOB" value={draft.fuelSaveLoadFob}
          onChange={(v) => update("fuelSaveLoadFob", v)} />
        <BoolField label="Randomize Pax" value={draft.randomizePax}
          onChange={(v) => update("randomizePax", v)} />
        <NumberField label="Chance Per Seat" value={draft.chancePerSeat} step={0.001}
          onChange={(v) => update("chancePerSeat", v)} />
      </Section>

      <Section title="Auto-Deice">
        <BoolField label="Auto-Deice Enabled" value={draft.autoDeiceEnabled}
          onChange={(v) => update("autoDeiceEnabled", v)} />
        <SelectField label="Deice Fluid" value={draft.autoDeiceFluid}
          options={AUTO_DEICE_FLUID_OPTIONS}
          onChange={(v) => update("autoDeiceFluid", v)} />
      </Section>
    </div>
  );
}

interface ListEditorProps {
  label: string;
  values: string[];
  onChangeAt: (idx: number, value: string) => void;
  onAdd: () => void;
  onRemove: (idx: number) => void;
}

function ListEditor({ label, values, onChangeAt, onAdd, onRemove }: ListEditorProps) {
  return (
    <div className={styles.listEditor}>
      <div className={styles.listLabel}>{label}</div>
      {values.length === 0 && <div className={styles.empty}>No entries.</div>}
      {values.map((v, i) => (
        <div key={i} className={styles.listRow}>
          <input value={v}
            onChange={(e) => onChangeAt(i, e.target.value)}
            className={styles.cellInput} />
          <button type="button" className={styles.removeBtn} onClick={() => onRemove(i)}>×</button>
        </div>
      ))}
      <PrimaryButton onClick={onAdd} variant="secondary">Add</PrimaryButton>
    </div>
  );
}
