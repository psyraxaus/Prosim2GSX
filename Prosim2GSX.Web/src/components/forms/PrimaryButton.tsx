import { ReactNode } from "react";
import styles from "./PrimaryButton.module.css";

interface Props {
  onClick?: () => void;
  disabled?: boolean;
  variant?: "primary" | "secondary" | "danger";
  type?: "button" | "submit";
  children: ReactNode;
}

export function PrimaryButton({ onClick, disabled, variant = "primary", type = "button", children }: Props) {
  return (
    <button
      type={type}
      onClick={onClick}
      disabled={disabled}
      className={`${styles.button} ${styles[variant]}`}
    >
      {children}
    </button>
  );
}
