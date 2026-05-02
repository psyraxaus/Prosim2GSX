import { Section } from "../../components/forms/Section";
import { PrimaryButton } from "../../components/forms/PrimaryButton";
import { GsxSectionProps } from "./sectionShared";
import styles from "../GsxSettingsPanel.module.css";

export function CompanyHubsSection({
  draft, updateListItem, addListItem, removeListItem,
}: GsxSectionProps) {
  return (
    <Section title="Company Hubs" hint="ICAO prefixes used to identify company-hub airports">
      {draft.companyHubs.length === 0 && (
        <div className={styles.empty}>No hubs configured.</div>
      )}
      {draft.companyHubs.map((v, i) => (
        <div key={i} className={styles.listRow}>
          <input value={v}
            onChange={(e) => updateListItem("companyHubs", i, e.target.value)}
            className={styles.cellInput}
            placeholder="ICAO prefix" />
          <button type="button" className={styles.removeBtn}
            onClick={() => removeListItem("companyHubs", i)}>×</button>
        </div>
      ))}
      <PrimaryButton onClick={() => addListItem("companyHubs")} variant="secondary">
        Add hub
      </PrimaryButton>
    </Section>
  );
}
