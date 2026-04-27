import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import styles from "./Section.module.css";
// Card-style grouping for related fields. Mirrors the WPF
// "CategoryHeader + SectionCardBorder" pattern.
export function Section({ title, hint, children }) {
    return (_jsxs("section", { className: styles.section, children: [_jsxs("div", { className: styles.header, children: [_jsx("h3", { className: styles.title, children: title }), hint && _jsx("span", { className: styles.hint, children: hint })] }), _jsx("div", { className: styles.body, children: children })] }));
}
