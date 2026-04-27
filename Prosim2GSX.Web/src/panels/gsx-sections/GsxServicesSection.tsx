import { Section } from "../../components/forms/Section";
import { BoolField, NumberField, SelectField } from "../../components/forms/Field";
import { PrimaryButton } from "../../components/forms/PrimaryButton";
import {
  AUTO_DEICE_FLUID_OPTIONS,
  PUSHBACK_TIMING_OPTIONS,
  REFUEL_METHOD_OPTIONS,
  SERVICE_ACTIVATION_OPTIONS,
  SERVICE_CONSTRAINT_OPTIONS,
  SERVICE_TYPE_OPTIONS,
  TUG_OPTIONS,
} from "../../types";
import { GsxSectionProps } from "./sectionShared";
import styles from "../GsxSettingsPanel.module.css";

// Mirrors the WPF "ControlGsxServices" surface: SERVICE CALLS, REFUEL,
// DE-ICING, PUSHBACK & TUG (and the beacon-orchestrated departure
// sequence which lives next to pushback in the WPF view).
export function GsxServicesSection({
  draft, update,
  updateService, moveService, removeService, addService,
}: GsxSectionProps) {
  return (
    <>
      <Section title="Service Calls">
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

      <Section title="De-Icing">
        <BoolField label="Auto-Deice Enabled" value={draft.autoDeiceEnabled}
          onChange={(v) => update("autoDeiceEnabled", v)} />
        <SelectField label="Deice Fluid" value={draft.autoDeiceFluid}
          options={AUTO_DEICE_FLUID_OPTIONS}
          onChange={(v) => update("autoDeiceFluid", v)} />
      </Section>

      <Section title="Pushback &amp; Tug">
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
    </>
  );
}
