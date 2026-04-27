import { Section } from "../../components/forms/Section";
import { BoolField, NumberField, SelectField } from "../../components/forms/Field";
import { CONNECT_PCA_OPTIONS } from "../../types";
import { GsxSectionProps } from "./sectionShared";

export function GroundEquipmentSection({ draft, update }: GsxSectionProps) {
  return (
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
  );
}
