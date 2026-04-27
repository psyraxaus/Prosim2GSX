import { useEffect, useMemo, useState } from "react";
import { useApi } from "../api/useApi";
import { DirtyBar } from "../components/forms/DirtyBar";
import { SectionItem, SectionNav } from "../components/SectionNav";
import { GsxSettingsDto, ServiceConfigDto } from "../types";
import { GateDoorsSection } from "./gsx-sections/GateDoorsSection";
import { GroundEquipmentSection } from "./gsx-sections/GroundEquipmentSection";
import { GsxServicesSection } from "./gsx-sections/GsxServicesSection";
import { OperatorSelectionSection } from "./gsx-sections/OperatorSelectionSection";
import { CompanyHubsSection } from "./gsx-sections/CompanyHubsSection";
import { SkipQuestionsSection } from "./gsx-sections/SkipQuestionsSection";
import { AircraftOptionsSection } from "./gsx-sections/AircraftOptionsSection";
import { GsxSectionProps } from "./gsx-sections/sectionShared";
import styles from "./GsxSettingsPanel.module.css";

// Section keys mirror the WPF ViewAutomation.SettingControl enum so the
// left-rail order matches the desktop UI: Gate & Doors → Ground Equipment
// → GSX Services → Operator Selection → Company Hubs → Skip Questions →
// Aircraft Options.
type SectionKey =
  | "gateDoors"
  | "groundEquip"
  | "gsxServices"
  | "operator"
  | "companyHubs"
  | "skipQuestions"
  | "aircraftOptions";

const SECTIONS: SectionItem<SectionKey>[] = [
  { key: "gateDoors", label: "Gate & Doors" },
  { key: "groundEquip", label: "Ground Equipment" },
  { key: "gsxServices", label: "GSX Services" },
  { key: "operator", label: "Operator Selection" },
  { key: "companyHubs", label: "Company Hubs" },
  { key: "skipQuestions", label: "Skip Questions" },
  { key: "aircraftOptions", label: "Aircraft Options" },
];

export function GsxSettingsPanel() {
  const { get, post } = useApi();

  // Shared draft state — every sub-section reads from / writes into this
  // single object so switching sections doesn't lose unsaved edits.
  const [draft, setDraft] = useState<GsxSettingsDto | null>(null);
  // The last server-confirmed snapshot. Used to detect dirty state and to
  // power the Discard button (replace draft with this snapshot).
  const [baseline, setBaseline] = useState<GsxSettingsDto | null>(null);
  const [activeSection, setActiveSection] = useState<SectionKey>("gateDoors");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);

  async function reload() {
    setError(null);
    try {
      const dto = await get<GsxSettingsDto>("/gsxsettings");
      setBaseline(dto);
      setDraft(dto);
    } catch (e: unknown) {
      setError((e as Error).message ?? "Failed to load");
    }
  }
  useEffect(() => { reload(); }, []);

  async function save() {
    if (!draft) return;
    setSaving(true); setError(null); setInfo(null);
    try {
      const fresh = await post<GsxSettingsDto>("/gsxsettings", draft);
      setBaseline(fresh);
      setDraft(fresh);
      setInfo("Saved.");
      setTimeout(() => setInfo(null), 1500);
    } catch (e: unknown) {
      setError((e as Error).message ?? "Save failed");
    } finally {
      setSaving(false);
    }
  }

  function discard() {
    setError(null);
    setInfo(null);
    if (baseline) setDraft(baseline);
  }

  // Cheap isDirty: deep-compare via JSON. DTOs are simple data so no
  // ordering / circular concerns. If draft and baseline reference the
  // same instance (just-saved state) the early-out is also free.
  const isDirty = useMemo(() => {
    if (!draft || !baseline) return false;
    if (draft === baseline) return false;
    return JSON.stringify(draft) !== JSON.stringify(baseline);
  }, [draft, baseline]);

  if (!draft) {
    return (
      <div className={styles.loading}>
        {error ? `Error: ${error}` : "Loading GSX settings…"}
      </div>
    );
  }

  // ── Helpers handed to sub-sections ──────────────────────────────────

  function update<K extends keyof GsxSettingsDto>(key: K, value: GsxSettingsDto[K]) {
    setDraft((d) => (d ? { ...d, [key]: value } : d));
  }

  function updateService(idx: number, partial: Partial<ServiceConfigDto>) {
    setDraft((d) => {
      if (!d) return d;
      const departureServices = d.departureServices.map((s, i) => (i === idx ? { ...s, ...partial } : s));
      return { ...d, departureServices };
    });
  }

  function moveService(idx: number, dir: -1 | 1) {
    setDraft((d) => {
      if (!d) return d;
      const next = idx + dir;
      if (next < 0 || next >= d.departureServices.length) return d;
      const services = [...d.departureServices];
      [services[idx], services[next]] = [services[next], services[idx]];
      return { ...d, departureServices: services };
    });
  }

  function removeService(idx: number) {
    setDraft((d) => (d ? { ...d, departureServices: d.departureServices.filter((_, i) => i !== idx) } : d));
  }

  function addService() {
    setDraft((d) => {
      if (!d) return d;
      const fresh: ServiceConfigDto = {
        serviceType: "Unknown",
        serviceActivation: "Manual",
        serviceConstraint: "NoneAlways",
        minimumFlightDuration: 0,
      };
      return { ...d, departureServices: [...d.departureServices, fresh] };
    });
  }

  function updateListItem(field: "operatorPreferences" | "companyHubs", idx: number, value: string) {
    setDraft((d) => {
      if (!d) return d;
      const list = d[field].map((s, i) => (i === idx ? value : s));
      return { ...d, [field]: list };
    });
  }
  function addListItem(field: "operatorPreferences" | "companyHubs") {
    setDraft((d) => (d ? { ...d, [field]: [...d[field], ""] } : d));
  }
  function removeListItem(field: "operatorPreferences" | "companyHubs", idx: number) {
    setDraft((d) => (d ? { ...d, [field]: d[field].filter((_, i) => i !== idx) } : d));
  }

  const sectionProps: GsxSectionProps = {
    draft,
    update,
    updateService, moveService, removeService, addService,
    updateListItem, addListItem, removeListItem,
  };

  return (
    <div className={styles.panel}>
      <div className={styles.toolbar}>
        {draft.profileName && (
          <span className={styles.profileName}>Profile: {draft.profileName}</span>
        )}
      </div>

      <div className={styles.body}>
        <SectionNav
          items={SECTIONS}
          active={activeSection}
          onSelect={setActiveSection}
        />
        <div className={styles.content}>
          {activeSection === "gateDoors" && <GateDoorsSection {...sectionProps} />}
          {activeSection === "groundEquip" && <GroundEquipmentSection {...sectionProps} />}
          {activeSection === "gsxServices" && <GsxServicesSection {...sectionProps} />}
          {activeSection === "operator" && <OperatorSelectionSection {...sectionProps} />}
          {activeSection === "companyHubs" && <CompanyHubsSection {...sectionProps} />}
          {activeSection === "skipQuestions" && <SkipQuestionsSection {...sectionProps} />}
          {activeSection === "aircraftOptions" && <AircraftOptionsSection {...sectionProps} />}
        </div>
      </div>

      <DirtyBar
        isDirty={isDirty}
        saving={saving}
        error={error}
        info={info}
        onSave={save}
        onDiscard={discard}
      />
    </div>
  );
}
