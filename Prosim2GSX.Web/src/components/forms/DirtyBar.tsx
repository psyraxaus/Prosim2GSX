import { PrimaryButton } from "./PrimaryButton";
import styles from "./DirtyBar.module.css";

interface Props {
  isDirty: boolean;
  saving: boolean;
  error?: string | null;
  info?: string | null;
  onSave: () => void;
  onDiscard: () => void;
}

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
export function DirtyBar({ isDirty, saving, error, info, onSave, onDiscard }: Props) {
  const visible = isDirty || !!error || !!info;
  return (
    <div className={`${styles.bar} ${visible ? styles.visible : ""}`} aria-hidden={!visible}>
      <div className={styles.inner}>
        <div className={styles.message}>
          {error && <span className={styles.error}>{error}</span>}
          {info && !error && <span className={styles.info}>{info}</span>}
          {!error && !info && isDirty && (
            <span className={styles.unsaved}>You have unsaved changes</span>
          )}
        </div>
        <div className={styles.actions}>
          <PrimaryButton
            variant="secondary"
            onClick={onDiscard}
            disabled={saving || !isDirty}
          >
            Discard
          </PrimaryButton>
          <PrimaryButton onClick={onSave} disabled={saving || !isDirty}>
            {saving ? "Saving…" : "Save"}
          </PrimaryButton>
        </div>
      </div>
    </div>
  );
}
