import { Section } from "../../components/forms/Section";
import { BoolField, NumberField } from "../../components/forms/Field";
import { GsxSectionProps } from "./sectionShared";

export function AircraftOptionsSection({ draft, update }: GsxSectionProps) {
  return (
    <Section title="Aircraft &amp; OFP Options">
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
  );
}
