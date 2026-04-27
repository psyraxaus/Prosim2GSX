import { jsx as _jsx } from "react/jsx-runtime";
import styles from "./PrimaryButton.module.css";
export function PrimaryButton({ onClick, disabled, variant = "primary", children }) {
    return (_jsx("button", { type: "button", onClick: onClick, disabled: disabled, className: `${styles.button} ${styles[variant]}`, children: children }));
}
