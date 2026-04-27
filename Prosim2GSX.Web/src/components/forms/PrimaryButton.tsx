import { ReactNode } from "react";
import styles from "./PrimaryButton.module.css";

interface Props {
  onClick: () => void;
  disabled?: boolean;
  variant?: "primary" | "secondary" | "danger";
  children: ReactNode;
}

export function PrimaryButton({ onClick, disabled, variant = "primary", children }: Props) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className={`${styles.button} ${styles[variant]}`}
    >
      {children}
    </button>
  );
}
