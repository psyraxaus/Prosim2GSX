import { Section } from "../../components/forms/Section";
import { BoolField } from "../../components/forms/Field";
import { PrimaryButton } from "../../components/forms/PrimaryButton";
import { GsxSectionProps } from "./sectionShared";
import styles from "../GsxSettingsPanel.module.css";

export function OperatorSelectionSection({
  draft, update, updateListItem, addListItem, removeListItem,
}: GsxSectionProps) {
  return (
    <Section title="Operator Selection">
      <BoolField label="Operator Auto-Select" value={draft.operatorAutoSelect}
        onChange={(v) => update("operatorAutoSelect", v)} />

      <div className={styles.listEditor}>
        <div className={styles.listLabel}>Operator Preferences</div>
        {draft.operatorPreferences.length === 0 && (
          <div className={styles.empty}>No entries.</div>
        )}
        {draft.operatorPreferences.map((v, i) => (
          <div key={i} className={styles.listRow}>
            <input value={v}
              onChange={(e) => updateListItem("operatorPreferences", i, e.target.value)}
              className={styles.cellInput} />
            <button type="button" className={styles.removeBtn}
              onClick={() => removeListItem("operatorPreferences", i)}>×</button>
          </div>
        ))}
        <PrimaryButton onClick={() => addListItem("operatorPreferences")} variant="secondary">
          Add
        </PrimaryButton>
      </div>
    </Section>
  );
}
