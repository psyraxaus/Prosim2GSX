import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import styles from "./Field.module.css";
// Two-column row: label left, control right. Stacks at narrow widths.
export function Field({ label, hint, children }) {
    return (_jsxs("div", { className: styles.field, children: [_jsxs("label", { className: styles.label, children: [label, hint && _jsx("span", { className: styles.hint, children: hint })] }), _jsx("div", { className: styles.control, children: children })] }));
}
export function BoolField({ label, hint, value, onChange, disabled }) {
    return (_jsx(Field, { label: label, hint: hint, children: _jsx("input", { type: "checkbox", checked: value, disabled: disabled, onChange: (e) => onChange(e.target.checked), className: styles.checkbox }) }));
}
export function NumberField({ label, hint, value, onChange, min, max, step, suffix, disabled, }) {
    return (_jsx(Field, { label: label, hint: hint, children: _jsxs("div", { className: styles.numberRow, children: [_jsx("input", { type: "number", value: Number.isFinite(value) ? value : 0, min: min, max: max, step: step, disabled: disabled, onChange: (e) => {
                        const v = e.target.value;
                        const parsed = v === "" ? 0 : Number(v);
                        if (Number.isFinite(parsed))
                            onChange(parsed);
                    }, className: styles.numberInput }), suffix && _jsx("span", { className: styles.suffix, children: suffix })] }) }));
}
export function TextField({ label, hint, value, onChange, placeholder, readOnly, monospace, disabled, }) {
    return (_jsx(Field, { label: label, hint: hint, children: _jsx("input", { type: "text", value: value, readOnly: readOnly, disabled: disabled, placeholder: placeholder, onChange: (e) => onChange(e.target.value), className: `${styles.textInput} ${monospace ? styles.monospace : ""}`, spellCheck: false, autoCapitalize: "off", autoComplete: "off" }) }));
}
export function SelectField({ label, hint, value, options, onChange, disabled, }) {
    return (_jsx(Field, { label: label, hint: hint, children: _jsx("select", { value: String(value), disabled: disabled, onChange: (e) => {
                const raw = e.target.value;
                const numeric = options[0] && typeof options[0].value === "number";
                onChange((numeric ? Number(raw) : raw));
            }, className: styles.select, children: options.map((o) => (_jsx("option", { value: String(o.value), children: o.label }, String(o.value)))) }) }));
}
