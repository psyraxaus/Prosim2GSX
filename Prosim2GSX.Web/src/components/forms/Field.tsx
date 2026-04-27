import { ChangeEvent, ReactNode } from "react";
import styles from "./Field.module.css";

interface FieldShellProps {
  label: string;
  hint?: string;
  children: ReactNode;
}

// Two-column row: label left, control right. Stacks at narrow widths.
export function Field({ label, hint, children }: FieldShellProps) {
  return (
    <div className={styles.field}>
      <label className={styles.label}>
        {label}
        {hint && <span className={styles.hint}>{hint}</span>}
      </label>
      <div className={styles.control}>{children}</div>
    </div>
  );
}

interface BoolFieldProps {
  label: string;
  hint?: string;
  value: boolean;
  onChange: (next: boolean) => void;
  disabled?: boolean;
}

export function BoolField({ label, hint, value, onChange, disabled }: BoolFieldProps) {
  return (
    <Field label={label} hint={hint}>
      <input
        type="checkbox"
        checked={value}
        disabled={disabled}
        onChange={(e: ChangeEvent<HTMLInputElement>) => onChange(e.target.checked)}
        className={styles.checkbox}
      />
    </Field>
  );
}

interface NumberFieldProps {
  label: string;
  hint?: string;
  value: number;
  onChange: (next: number) => void;
  min?: number;
  max?: number;
  step?: number;
  suffix?: string;
  disabled?: boolean;
}

export function NumberField({
  label, hint, value, onChange, min, max, step, suffix, disabled,
}: NumberFieldProps) {
  return (
    <Field label={label} hint={hint}>
      <div className={styles.numberRow}>
        <input
          type="number"
          value={Number.isFinite(value) ? value : 0}
          min={min}
          max={max}
          step={step}
          disabled={disabled}
          onChange={(e) => {
            const v = e.target.value;
            const parsed = v === "" ? 0 : Number(v);
            if (Number.isFinite(parsed)) onChange(parsed);
          }}
          className={styles.numberInput}
        />
        {suffix && <span className={styles.suffix}>{suffix}</span>}
      </div>
    </Field>
  );
}

interface TextFieldProps {
  label: string;
  hint?: string;
  value: string;
  onChange: (next: string) => void;
  placeholder?: string;
  readOnly?: boolean;
  monospace?: boolean;
  disabled?: boolean;
}

export function TextField({
  label, hint, value, onChange, placeholder, readOnly, monospace, disabled,
}: TextFieldProps) {
  return (
    <Field label={label} hint={hint}>
      <input
        type="text"
        value={value}
        readOnly={readOnly}
        disabled={disabled}
        placeholder={placeholder}
        onChange={(e) => onChange(e.target.value)}
        className={`${styles.textInput} ${monospace ? styles.monospace : ""}`}
        spellCheck={false}
        autoCapitalize="off"
        autoComplete="off"
      />
    </Field>
  );
}

interface SelectFieldProps<T extends string | number> {
  label: string;
  hint?: string;
  value: T;
  options: { value: T; label: string }[];
  onChange: (next: T) => void;
  disabled?: boolean;
}

export function SelectField<T extends string | number>({
  label, hint, value, options, onChange, disabled,
}: SelectFieldProps<T>) {
  return (
    <Field label={label} hint={hint}>
      <select
        value={String(value)}
        disabled={disabled}
        onChange={(e) => {
          const raw = e.target.value;
          const numeric = options[0] && typeof options[0].value === "number";
          onChange((numeric ? Number(raw) : raw) as T);
        }}
        className={styles.select}
      >
        {options.map((o) => (
          <option key={String(o.value)} value={String(o.value)}>
            {o.label}
          </option>
        ))}
      </select>
    </Field>
  );
}
