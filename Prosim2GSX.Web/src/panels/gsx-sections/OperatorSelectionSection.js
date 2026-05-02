import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { Section } from "../../components/forms/Section";
import { BoolField } from "../../components/forms/Field";
import { PrimaryButton } from "../../components/forms/PrimaryButton";
import styles from "../GsxSettingsPanel.module.css";
export function OperatorSelectionSection({ draft, update, updateListItem, addListItem, removeListItem, }) {
    return (_jsxs(Section, { title: "Operator Selection", children: [_jsx(BoolField, { label: "Operator Auto-Select", value: draft.operatorAutoSelect, onChange: (v) => update("operatorAutoSelect", v) }), _jsxs("div", { className: styles.listEditor, children: [_jsx("div", { className: styles.listLabel, children: "Operator Preferences" }), draft.operatorPreferences.length === 0 && (_jsx("div", { className: styles.empty, children: "No entries." })), draft.operatorPreferences.map((v, i) => (_jsxs("div", { className: styles.listRow, children: [_jsx("input", { value: v, onChange: (e) => updateListItem("operatorPreferences", i, e.target.value), className: styles.cellInput }), _jsx("button", { type: "button", className: styles.removeBtn, onClick: () => removeListItem("operatorPreferences", i), children: "\u00D7" })] }, i))), _jsx(PrimaryButton, { onClick: () => addListItem("operatorPreferences"), variant: "secondary", children: "Add" })] })] }));
}
