import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { Section } from "../../components/forms/Section";
import { PrimaryButton } from "../../components/forms/PrimaryButton";
import styles from "../GsxSettingsPanel.module.css";
export function CompanyHubsSection({ draft, updateListItem, addListItem, removeListItem, }) {
    return (_jsxs(Section, { title: "Company Hubs", hint: "ICAO prefixes used to identify company-hub airports", children: [draft.companyHubs.length === 0 && (_jsx("div", { className: styles.empty, children: "No hubs configured." })), draft.companyHubs.map((v, i) => (_jsxs("div", { className: styles.listRow, children: [_jsx("input", { value: v, onChange: (e) => updateListItem("companyHubs", i, e.target.value), className: styles.cellInput, placeholder: "ICAO prefix" }), _jsx("button", { type: "button", className: styles.removeBtn, onClick: () => removeListItem("companyHubs", i), children: "\u00D7" })] }, i))), _jsx(PrimaryButton, { onClick: () => addListItem("companyHubs"), variant: "secondary", children: "Add hub" })] }));
}
