import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { PrimaryButton } from "./PrimaryButton";
import styles from "./DirtyBar.module.css";
// Sticky save bar that slides in fixed to the bottom of the viewport when
// the form has unsaved changes. Hides entirely when clean — see project
// memory phase8_design_decisions ("hide when clean and slide in when first
// edit"). Buttons rename: "Discard" while dirty (matches user decision),
// the underlying behaviour is the same as the previous Reload button
// (refetch from server, drop local edits) — implemented by the parent
// panel's onDiscard handler.
//
// Stays visible to surface error / info messages even when not dirty so
// the result of a save is briefly observable on the same component.
export function DirtyBar({ isDirty, saving, error, info, onSave, onDiscard }) {
    const visible = isDirty || !!error || !!info;
    return (_jsx("div", { className: `${styles.bar} ${visible ? styles.visible : ""}`, "aria-hidden": !visible, children: _jsxs("div", { className: styles.inner, children: [_jsxs("div", { className: styles.message, children: [error && _jsx("span", { className: styles.error, children: error }), info && !error && _jsx("span", { className: styles.info, children: info }), !error && !info && isDirty && (_jsx("span", { className: styles.unsaved, children: "You have unsaved changes" }))] }), _jsxs("div", { className: styles.actions, children: [_jsx(PrimaryButton, { variant: "secondary", onClick: onDiscard, disabled: saving || !isDirty, children: "Discard" }), _jsx(PrimaryButton, { onClick: onSave, disabled: saving || !isDirty, children: saving ? "Saving…" : "Save" })] })] }) }));
}
