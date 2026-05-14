import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useEffect, useState } from "react";
import styles from "./PerfShared.module.css";
export function KeyboardNumberInput({ value, onCommit, unit, step = 1, min, max, integer = false, width = 5, disabled, ariaLabel, }) {
    const [text, setText] = useState(formatNumber(value, integer));
    // Re-sync the input when the prop changes externally (WS patch, /sync,
    // reset). Skip when the input is currently focused so user typing
    // isn't yanked out from under them.
    useEffect(() => {
        if (document.activeElement?.tagName === "INPUT") {
            const focused = document.activeElement;
            if (focused.value === text)
                return;
        }
        setText(formatNumber(value, integer));
    }, [value, integer]); // eslint-disable-line react-hooks/exhaustive-deps
    function commit() {
        const parsed = integer ? parseInt(text, 10) : parseFloat(text);
        if (Number.isNaN(parsed)) {
            setText(formatNumber(value, integer));
            return;
        }
        let clamped = parsed;
        if (min !== undefined)
            clamped = Math.max(min, clamped);
        if (max !== undefined)
            clamped = Math.min(max, clamped);
        setText(formatNumber(clamped, integer));
        if (clamped !== value)
            onCommit(clamped);
    }
    return (_jsxs("span", { className: styles.kbdNumber, children: [_jsx("input", { type: "number", value: text, step: step, min: min, max: max, disabled: disabled, "aria-label": ariaLabel, style: { width: `${width}ch` }, onChange: (e) => setText(e.target.value), onBlur: commit, onKeyDown: (e) => {
                    if (e.key === "Enter")
                        e.target.blur();
                } }), unit && _jsx("span", { className: styles.unit, children: unit })] }));
}
function formatNumber(n, integer) {
    if (!Number.isFinite(n))
        return "0";
    return integer ? String(Math.round(n)) : String(n);
}
