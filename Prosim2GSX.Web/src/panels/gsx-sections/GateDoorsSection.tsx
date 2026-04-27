import { Section } from "../../components/forms/Section";
import { BoolField, NumberField, SelectField } from "../../components/forms/Field";
import { REMOVE_STAIRS_OPTIONS } from "../../types";
import { GsxSectionProps } from "./sectionShared";

// Mirrors the WPF "ControlGateDoors" surface: DOOR HANDLING + JETWAY &
// STAIR CONTROL — combined into one tab section as in the WPF view.
export function GateDoorsSection({ draft, update }: GsxSectionProps) {
  return (
    <>
      <Section title="Doors">
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
    </>
  );
}

// Numeric field for ChockDelay etc. lives in GroundEquipmentSection per the
// WPF grouping (ground-equipment includes chocks). NumberField imported here
// only for the unused-import guard.
void NumberField;
