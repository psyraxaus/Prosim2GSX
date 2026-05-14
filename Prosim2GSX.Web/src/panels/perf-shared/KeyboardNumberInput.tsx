import { ChangeEvent, useEffect, useState } from "react";
import styles from "./PerfShared.module.css";

// EFB-style right-aligned numeric input with an optional trailing unit
// chip. Commits on blur or Enter — the panel layer is responsible for
// debouncing the resulting POST to /api/perf/.../inputs.
//
// The internal `text` state lets the user type freely (including
// negative signs and incomplete decimals) without the value bouncing
// back to the props until the field commits. NaN-only inputs are
// discarded on commit so the parent state never sees garbage.

interface Props {
  value: number;
  onCommit: (next: number) => void;
  unit?: string;
  step?: number;
  min?: number;
  max?: number;
  integer?: boolean;
  width?: number;        // chars
  disabled?: boolean;
  ariaLabel?: string;
}

export function KeyboardNumberInput({
  value,
  onCommit,
  unit,
  step = 1,
  min,
  max,
  integer = false,
  width = 5,
  disabled,
  ariaLabel,
}: Props) {
  const [text, setText] = useState<string>(formatNumber(value, integer));

  // Re-sync the input when the prop changes externally (WS patch, /sync,
  // reset). Skip when the input is currently focused so user typing
  // isn't yanked out from under them.
  useEffect(() => {
    if (document.activeElement?.tagName === "INPUT") {
      const focused = document.activeElement as HTMLInputElement;
      if (focused.value === text) return;
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
    if (min !== undefined) clamped = Math.max(min, clamped);
    if (max !== undefined) clamped = Math.min(max, clamped);
    setText(formatNumber(clamped, integer));
    if (clamped !== value) onCommit(clamped);
  }

  return (
    <span className={styles.kbdNumber}>
      <input
        type="number"
        value={text}
        step={step}
        min={min}
        max={max}
        disabled={disabled}
        aria-label={ariaLabel}
        style={{ width: `${width}ch` }}
        onChange={(e: ChangeEvent<HTMLInputElement>) => setText(e.target.value)}
        onBlur={commit}
        onKeyDown={(e) => {
          if (e.key === "Enter") (e.target as HTMLInputElement).blur();
        }}
      />
      {unit && <span className={styles.unit}>{unit}</span>}
    </span>
  );
}

function formatNumber(n: number, integer: boolean): string {
  if (!Number.isFinite(n)) return "0";
  return integer ? String(Math.round(n)) : String(n);
}
