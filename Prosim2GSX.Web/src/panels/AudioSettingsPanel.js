import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useEffect, useState } from "react";
import { useApi } from "../api/useApi";
import { Section } from "../components/forms/Section";
import { RadioField, SelectField } from "../components/forms/Field";
import { PrimaryButton } from "../components/forms/PrimaryButton";
import { ACP_SIDE_OPTIONS, AUDIO_CHANNELS, DATA_FLOW_OPTIONS, DEVICE_STATE_OPTIONS, } from "../types";
import styles from "./AudioSettingsPanel.module.css";
const ELEVATED_SUFFIX = " — elevated";
export function AudioSettingsPanel() {
    const { get, post } = useApi();
    const [draft, setDraft] = useState(null);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState(null);
    const [info, setInfo] = useState(null);
    const [suggestions, setSuggestions] = useState([]);
    async function reload() {
        setError(null);
        try {
            const dto = await get("/audio");
            setDraft(dto);
        }
        catch (e) {
            setError(e.message ?? "Failed to load");
        }
    }
    async function reloadSuggestions() {
        try {
            const list = await get("/audio/process-suggestions");
            setSuggestions(list ?? []);
        }
        catch { /* leave whatever we had; field still accepts free text */ }
    }
    useEffect(() => {
        reload();
        reloadSuggestions();
    }, []);
    async function save() {
        if (!draft)
            return;
        setSaving(true);
        setError(null);
        setInfo(null);
        try {
            const fresh = await post("/audio", draft);
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
    if (!draft) {
        return _jsx("div", { className: styles.loading, children: error ? `Error: ${error}` : "Loading audio settings…" });
    }
    function update(key, value) {
        setDraft((d) => (d ? { ...d, [key]: value } : d));
    }
    function updateMapping(idx, partial) {
        setDraft((d) => {
            if (!d)
                return d;
            const mappings = d.mappings.map((m, i) => (i === idx ? { ...m, ...partial } : m));
            return { ...d, mappings };
        });
    }
    function removeMapping(idx) {
        setDraft((d) => (d ? { ...d, mappings: d.mappings.filter((_, i) => i !== idx) } : d));
    }
    function addMapping() {
        setDraft((d) => {
            if (!d)
                return d;
            const fresh = {
                channel: "VHF1",
                device: "",
                binary: "",
                useLatch: true,
                onlyActive: true,
            };
            return { ...d, mappings: [...d.mappings, fresh] };
        });
    }
    function updateBlacklist(idx, value) {
        setDraft((d) => {
            if (!d)
                return d;
            const blacklist = d.blacklist.map((s, i) => (i === idx ? value : s));
            return { ...d, blacklist };
        });
    }
    function addBlacklist() {
        setDraft((d) => (d ? { ...d, blacklist: [...d.blacklist, ""] } : d));
    }
    function removeBlacklist(idx) {
        setDraft((d) => (d ? { ...d, blacklist: d.blacklist.filter((_, i) => i !== idx) } : d));
    }
    // Per-mapping elevated-status derived from suggestions: matching binary
    // entry that is currently NOT accessible. Mirrors the WPF Status column /
    // banner without needing a transient field on the DTO.
    const suggestionByName = new Map(suggestions.map((s) => [s.processName.toLowerCase(), s]));
    const elevatedBinaries = Array.from(new Set(draft.mappings
        .map((m) => m.binary)
        .filter((b) => {
        const match = suggestionByName.get((b ?? "").toLowerCase());
        return match != null && !match.isAccessible;
    })));
    return (_jsxs("div", { className: styles.panel, children: [_jsxs("div", { className: styles.toolbar, children: [_jsx(PrimaryButton, { onClick: save, disabled: saving, children: "Save" }), _jsx(PrimaryButton, { onClick: reload, variant: "secondary", disabled: saving, children: "Reload" }), _jsxs("div", { className: styles.toolbarStatus, children: [error && _jsx("span", { className: styles.error, children: error }), info && _jsx("span", { className: styles.info, children: info })] })] }), _jsxs(Section, { title: "Audio API", children: [_jsx(RadioField, { label: "Backend", name: "audioApi", value: draft.isCoreAudioSelected ? "core" : "voicemeeter", options: [
                            { value: "core", label: "Core Audio (Process Control)" },
                            { value: "voicemeeter", label: "VoiceMeeter API (Strip Control)" },
                        ], onChange: (v) => update("isCoreAudioSelected", v === "core") }), _jsx(SelectField, { label: "ACP Side", value: draft.audioAcpSide, options: ACP_SIDE_OPTIONS, onChange: (v) => update("audioAcpSide", v) }), _jsx(SelectField, { label: "Device Flow", value: draft.audioDeviceFlow, options: DATA_FLOW_OPTIONS, onChange: (v) => update("audioDeviceFlow", v) }), _jsx(SelectField, { label: "Device State", value: draft.audioDeviceState, options: DEVICE_STATE_OPTIONS, onChange: (v) => update("audioDeviceState", v) })] }), _jsxs(Section, { title: "App \u2192 Channel Mappings", children: [elevatedBinaries.length > 0 && (_jsxs("div", { className: styles.warningBanner, children: ["Elevated process(es) detected: ", elevatedBinaries.join(", "), ". Run Prosim2GSX as administrator to control these apps \u2014 otherwise these mappings are inactive."] })), _jsxs("div", { className: styles.mappingsHeader, children: [_jsx("span", { children: "Channel" }), _jsx("span", { children: "Binary" }), _jsx("span", { children: "Device" }), _jsx("span", { children: "Latch" }), _jsx("span", { children: "Active" }), _jsx("span", { children: "Status" }), _jsx("span", {})] }), draft.mappings.length === 0 && _jsx("div", { className: styles.empty, children: "No mappings configured." }), draft.mappings.map((m, i) => {
                        const match = suggestionByName.get((m.binary ?? "").toLowerCase());
                        const isElevated = match != null && !match.isAccessible;
                        return (_jsxs("div", { className: styles.mappingRow, children: [_jsx("select", { value: m.channel, onChange: (e) => updateMapping(i, { channel: e.target.value }), className: styles.cellSelect, children: AUDIO_CHANNELS.map((c) => _jsx("option", { value: c, children: c }, c)) }), _jsx("input", { value: m.binary, list: "audio-process-suggestions", onFocus: reloadSuggestions, onChange: (e) => {
                                        let v = e.target.value;
                                        if (v.endsWith(ELEVATED_SUFFIX))
                                            v = v.slice(0, -ELEVATED_SUFFIX.length);
                                        updateMapping(i, { binary: v });
                                    }, placeholder: "ProcessName", className: styles.cellInput }), _jsx("input", { value: m.device, onChange: (e) => updateMapping(i, { device: e.target.value }), placeholder: "(All)", className: styles.cellInput }), _jsx("input", { type: "checkbox", checked: m.useLatch, onChange: (e) => updateMapping(i, { useLatch: e.target.checked }) }), _jsx("input", { type: "checkbox", checked: m.onlyActive, onChange: (e) => updateMapping(i, { onlyActive: e.target.checked }) }), _jsx("span", { className: styles.statusCell, title: isElevated ? "Elevated — run Prosim2GSX as admin" : "", children: isElevated ? "Elevated — run Prosim2GSX as admin" : "" }), _jsx("button", { type: "button", className: styles.removeBtn, onClick: () => removeMapping(i), children: "\u00D7" })] }, i));
                    }), _jsx(PrimaryButton, { onClick: addMapping, variant: "secondary", children: "Add mapping" })] }), _jsx("datalist", { id: "audio-process-suggestions", children: suggestions.map((s) => (_jsx("option", { value: s.isAccessible ? s.processName : s.processName + ELEVATED_SUFFIX }, s.processName))) }), _jsxs(Section, { title: "Device Blacklist", children: [draft.blacklist.length === 0 && _jsx("div", { className: styles.empty, children: "No devices blacklisted." }), draft.blacklist.map((d, i) => (_jsxs("div", { className: styles.blacklistRow, children: [_jsx("input", { value: d, onChange: (e) => updateBlacklist(i, e.target.value), placeholder: "Device name", className: styles.cellInput }), _jsx("button", { type: "button", className: styles.removeBtn, onClick: () => removeBlacklist(i), children: "\u00D7" })] }, i))), _jsx(PrimaryButton, { onClick: addBlacklist, variant: "secondary", children: "Add device" })] })] }));
}
