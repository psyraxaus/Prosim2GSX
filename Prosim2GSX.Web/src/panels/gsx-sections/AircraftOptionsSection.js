import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { Section } from "../../components/forms/Section";
import { BoolField, NumberField } from "../../components/forms/Field";
export function AircraftOptionsSection({ draft, update }) {
    return (_jsxs(Section, { title: "Aircraft & OFP Options", children: [_jsx(NumberField, { label: "Final Delay Min (s)", value: draft.finalDelayMin, onChange: (v) => update("finalDelayMin", v) }), _jsx(NumberField, { label: "Final Delay Max (s)", value: draft.finalDelayMax, onChange: (v) => update("finalDelayMax", v) }), _jsx(BoolField, { label: "Save / Load FOB", value: draft.fuelSaveLoadFob, onChange: (v) => update("fuelSaveLoadFob", v) }), _jsx(BoolField, { label: "Randomize Pax", value: draft.randomizePax, onChange: (v) => update("randomizePax", v) }), _jsx(NumberField, { label: "Chance Per Seat", value: draft.chancePerSeat, step: 0.001, onChange: (v) => update("chancePerSeat", v) })] }));
}
