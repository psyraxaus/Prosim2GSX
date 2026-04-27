import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useEffect, useMemo, useState } from "react";
import { useApi } from "../api/useApi";
import { DirtyBar } from "../components/forms/DirtyBar";
import { SectionNav } from "../components/SectionNav";
import { GateDoorsSection } from "./gsx-sections/GateDoorsSection";
import { GroundEquipmentSection } from "./gsx-sections/GroundEquipmentSection";
import { GsxServicesSection } from "./gsx-sections/GsxServicesSection";
import { OperatorSelectionSection } from "./gsx-sections/OperatorSelectionSection";
import { CompanyHubsSection } from "./gsx-sections/CompanyHubsSection";
import { SkipQuestionsSection } from "./gsx-sections/SkipQuestionsSection";
import { AircraftOptionsSection } from "./gsx-sections/AircraftOptionsSection";
import styles from "./GsxSettingsPanel.module.css";
const SECTIONS = [
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
    const [draft, setDraft] = useState(null);
    // The last server-confirmed snapshot. Used to detect dirty state and to
    // power the Discard button (replace draft with this snapshot).
    const [baseline, setBaseline] = useState(null);
    const [activeSection, setActiveSection] = useState("gateDoors");
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState(null);
    const [info, setInfo] = useState(null);
    async function reload() {
        setError(null);
        try {
            const dto = await get("/gsxsettings");
            setBaseline(dto);
            setDraft(dto);
        }
        catch (e) {
            setError(e.message ?? "Failed to load");
        }
    }
    useEffect(() => { reload(); }, []);
    async function save() {
        if (!draft)
            return;
        setSaving(true);
        setError(null);
        setInfo(null);
        try {
            const fresh = await post("/gsxsettings", draft);
            setBaseline(fresh);
            setDraft(fresh);
            setInfo("Saved.");
            setTimeout(() => setInfo(null), 1500);
        }
        catch (e) {
            setError(e.message ?? "Save failed");
        }
        finally {
            setSaving(false);
        }
    }
    function discard() {
        setError(null);
        setInfo(null);
        if (baseline)
            setDraft(baseline);
    }
    // Cheap isDirty: deep-compare via JSON. DTOs are simple data so no
    // ordering / circular concerns. If draft and baseline reference the
    // same instance (just-saved state) the early-out is also free.
    const isDirty = useMemo(() => {
        if (!draft || !baseline)
            return false;
        if (draft === baseline)
            return false;
        return JSON.stringify(draft) !== JSON.stringify(baseline);
    }, [draft, baseline]);
    if (!draft) {
        return (_jsx("div", { className: styles.loading, children: error ? `Error: ${error}` : "Loading GSX settings…" }));
    }
    // ── Helpers handed to sub-sections ──────────────────────────────────
    function update(key, value) {
        setDraft((d) => (d ? { ...d, [key]: value } : d));
    }
    function updateService(idx, partial) {
        setDraft((d) => {
            if (!d)
                return d;
            const departureServices = d.departureServices.map((s, i) => (i === idx ? { ...s, ...partial } : s));
            return { ...d, departureServices };
        });
    }
    function moveService(idx, dir) {
        setDraft((d) => {
            if (!d)
                return d;
            const next = idx + dir;
            if (next < 0 || next >= d.departureServices.length)
                return d;
            const services = [...d.departureServices];
            [services[idx], services[next]] = [services[next], services[idx]];
            return { ...d, departureServices: services };
        });
    }
    function removeService(idx) {
        setDraft((d) => (d ? { ...d, departureServices: d.departureServices.filter((_, i) => i !== idx) } : d));
    }
    function addService() {
        setDraft((d) => {
            if (!d)
                return d;
            const fresh = {
                serviceType: "Unknown",
                serviceActivation: "Manual",
                serviceConstraint: "NoneAlways",
                minimumFlightDuration: 0,
            };
            return { ...d, departureServices: [...d.departureServices, fresh] };
        });
    }
    function updateListItem(field, idx, value) {
        setDraft((d) => {
            if (!d)
                return d;
            const list = d[field].map((s, i) => (i === idx ? value : s));
            return { ...d, [field]: list };
        });
    }
    function addListItem(field) {
        setDraft((d) => (d ? { ...d, [field]: [...d[field], ""] } : d));
    }
    function removeListItem(field, idx) {
        setDraft((d) => (d ? { ...d, [field]: d[field].filter((_, i) => i !== idx) } : d));
    }
    const sectionProps = {
        draft,
        update,
        updateService, moveService, removeService, addService,
        updateListItem, addListItem, removeListItem,
    };
    return (_jsxs("div", { className: styles.panel, children: [_jsx("div", { className: styles.toolbar, children: draft.profileName && (_jsxs("span", { className: styles.profileName, children: ["Profile: ", draft.profileName] })) }), _jsxs("div", { className: styles.body, children: [_jsx(SectionNav, { items: SECTIONS, active: activeSection, onSelect: setActiveSection }), _jsxs("div", { className: styles.content, children: [activeSection === "gateDoors" && _jsx(GateDoorsSection, { ...sectionProps }), activeSection === "groundEquip" && _jsx(GroundEquipmentSection, { ...sectionProps }), activeSection === "gsxServices" && _jsx(GsxServicesSection, { ...sectionProps }), activeSection === "operator" && _jsx(OperatorSelectionSection, { ...sectionProps }), activeSection === "companyHubs" && _jsx(CompanyHubsSection, { ...sectionProps }), activeSection === "skipQuestions" && _jsx(SkipQuestionsSection, { ...sectionProps }), activeSection === "aircraftOptions" && _jsx(AircraftOptionsSection, { ...sectionProps })] })] }), _jsx(DirtyBar, { isDirty: isDirty, saving: saving, error: error, info: info, onSave: save, onDiscard: discard })] }));
}
