import { jsx as _jsx, jsxs as _jsxs, Fragment as _Fragment } from "react/jsx-runtime";
import { useEffect, useState } from "react";
import { useApi } from "../api/useApi";
import { Section } from "../components/forms/Section";
import { RadioField, SelectField } from "../components/forms/Field";
import { PrimaryButton } from "../components/forms/PrimaryButton";
import { ACP_SIDE_LABELS, ACP_SIDE_OPTIONS, ACP_SIDES_ORDERED, AUDIO_CHANNELS, DATA_FLOW_OPTIONS, DEVICE_STATE_OPTIONS, } from "../types";
import styles from "./AudioSettingsPanel.module.css";
const ELEVATED_SUFFIX = " — elevated";
export function AudioSettingsPanel() {
    const { get, post } = useApi();
    const [draft, setDraft] = useState(null);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState(null);
    const [info, setInfo] = useState(null);
    const [suggestions, setSuggestions] = useState([]);
    const [strips, setStrips] = useState([]);
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
    async function reloadStrips() {
        try {
            const list = await get("/voicemeeter/strips");
            setStrips(list ?? []);
        }
        catch {
            setStrips([]);
        }
    }
    useEffect(() => {
        reload();
        reloadSuggestions();
        reloadStrips();
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
    function acpMappings(d, acp) {
        return d.voiceMeeterMappingsByAcp?.[acp] ?? [];
    }
    function setAcpMappings(d, acp, list) {
        return {
            ...d,
            voiceMeeterMappingsByAcp: { ...(d.voiceMeeterMappingsByAcp ?? {}), [acp]: list },
        };
    }
    function updateVmMapping(acp, idx, partial) {
        setDraft((d) => {
            if (!d)
                return d;
            const list = acpMappings(d, acp).map((m, i) => (i === idx ? { ...m, ...partial } : m));
            return setAcpMappings(d, acp, list);
        });
    }
    function removeVmMapping(acp, idx) {
        setDraft((d) => (d ? setAcpMappings(d, acp, acpMappings(d, acp).filter((_, i) => i !== idx)) : d));
    }
    function addVmMapping(acp) {
        setDraft((d) => {
            if (!d)
                return d;
            const fresh = {
                channel: "VHF1",
                stripIndex: 0,
                isBus: false,
                useLatch: true,
            };
            return setAcpMappings(d, acp, [...acpMappings(d, acp), fresh]);
        });
    }
    function setVmMappingTargetFromKey(acp, idx, key) {
        if (!key)
            return;
        const colon = key.indexOf(":");
        if (colon <= 0)
            return;
        const isBus = key.slice(0, colon) === "bus";
        const stripIdx = parseInt(key.slice(colon + 1), 10);
        if (Number.isNaN(stripIdx))
            return;
        updateVmMapping(acp, idx, { stripIndex: stripIdx, isBus });
    }
    function vmMappingKey(m) {
        return `${m.isBus ? "bus" : "strip"}:${m.stripIndex}`;
    }
    function toggleAcp(acp, checked) {
        setDraft((d) => {
            if (!d)
                return d;
            const cur = d.activeAcps ?? [];
            if (checked) {
                if (cur.includes(acp))
                    return d;
                return { ...d, activeAcps: [...cur, acp] };
            }
            // Min-1 invariant: silently reject the uncheck if it would empty the list.
            if (!cur.includes(acp) || cur.length <= 1)
                return d;
            return { ...d, activeAcps: cur.filter((s) => s !== acp) };
        });
    }
    // Mirrors VoiceMeeterMappingValidator.cs: within-ACP channel duplicates +
    // across-ACP strip duplicates. Returns null when valid.
    function validateVmMappings(d) {
        const conflicts = [];
        const stripOwner = new Map();
        for (const acp of d.activeAcps ?? []) {
            const list = acpMappings(d, acp);
            const channelSeen = new Set();
            for (const m of list) {
                if (channelSeen.has(m.channel))
                    conflicts.push(`${ACP_SIDE_LABELS[acp]} has duplicate channel ${m.channel}`);
                channelSeen.add(m.channel);
            }
            for (const m of list) {
                const key = `${m.isBus ? "bus" : "strip"}:${m.stripIndex}`;
                const existing = stripOwner.get(key);
                if (existing) {
                    const target = `${m.isBus ? "Bus" : "Strip"} ${m.stripIndex + 1}`;
                    conflicts.push(`${target} used by both ${ACP_SIDE_LABELS[existing.acp]}.${existing.ch} and ${ACP_SIDE_LABELS[acp]}.${m.channel}`);
                }
                else {
                    stripOwner.set(key, { acp, ch: m.channel });
                }
            }
        }
        return conflicts.length > 0 ? conflicts.join("; ") : null;
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
    const vmEnabled = draft.useVoiceMeeter;
    const vmAvailable = strips.length > 0;
    const vmWarning = !vmEnabled
        ? ""
        : !draft.voiceMeeterDllPath
            ? "VoiceMeeter integration is enabled but no DLL path is configured. Set the path below."
            : !vmAvailable
                ? "VoiceMeeter is not running or the Remote API DLL was not found."
                : "";
    // Live conflict check + active-ACP set, computed once per render.
    const vmConflict = vmEnabled ? validateVmMappings(draft) : null;
    const activeAcps = (draft.activeAcps ?? []);
    function setBackend(useVm) {
        setDraft((d) => (d ? { ...d, useVoiceMeeter: useVm, isCoreAudioSelected: !useVm } : d));
        if (useVm)
            reloadStrips();
    }
    return (_jsxs("div", { className: styles.panel, children: [_jsxs("div", { className: styles.toolbar, children: [_jsx(PrimaryButton, { onClick: save, disabled: saving || !!vmConflict, children: "Save" }), _jsx(PrimaryButton, { onClick: reload, variant: "secondary", disabled: saving, children: "Reload" }), _jsxs("div", { className: styles.toolbarStatus, children: [error && _jsx("span", { className: styles.error, children: error }), info && _jsx("span", { className: styles.info, children: info })] })] }), _jsxs(Section, { title: "Audio API", children: [_jsx(RadioField, { label: "Backend", name: "audioApi", value: draft.useVoiceMeeter ? "voicemeeter" : "core", options: [
                            { value: "core", label: "Core Audio (Process Control)" },
                            { value: "voicemeeter", label: "VoiceMeeter API (Strip Control)" },
                        ], onChange: (v) => setBackend(v === "voicemeeter") }), !vmEnabled && (_jsxs(_Fragment, { children: [_jsx(SelectField, { label: "ACP Side", value: draft.audioAcpSide, options: ACP_SIDE_OPTIONS, onChange: (v) => update("audioAcpSide", v) }), _jsx(SelectField, { label: "Device Flow", value: draft.audioDeviceFlow, options: DATA_FLOW_OPTIONS, onChange: (v) => update("audioDeviceFlow", v) }), _jsx(SelectField, { label: "Device State", value: draft.audioDeviceState, options: DEVICE_STATE_OPTIONS, onChange: (v) => update("audioDeviceState", v) })] }))] }), _jsxs(Section, { title: "VoiceMeeter", children: [vmWarning && _jsx("div", { className: styles.warningBanner, children: vmWarning }), _jsxs("div", { className: styles.vmPathRow, children: [_jsx("label", { className: styles.vmPathLabel, children: "Remote DLL path" }), _jsx("input", { type: "text", value: draft.voiceMeeterDllPath, placeholder: "C:\\Program Files (x86)\\VB\\Voicemeeter\\VoicemeeterRemote64.dll", onChange: (e) => update("voiceMeeterDllPath", e.target.value), className: styles.cellInput, spellCheck: false }), _jsx("button", { type: "button", className: styles.vmReloadBtn, onClick: reloadStrips, children: "Reload strips" })] })] }), !vmEnabled && (_jsxs(Section, { title: "App \u2192 Channel Mappings", children: [elevatedBinaries.length > 0 && (_jsxs("div", { className: styles.warningBanner, children: ["Elevated process(es) detected: ", elevatedBinaries.join(", "), ". Run Prosim2GSX as administrator to control these apps \u2014 otherwise these mappings are inactive."] })), _jsxs("div", { className: styles.mappingsHeader, children: [_jsx("span", { children: "Channel" }), _jsx("span", { children: "Binary" }), _jsx("span", { children: "Device" }), _jsx("span", { children: "Latch" }), _jsx("span", { children: "Active" }), _jsx("span", { children: "Status" }), _jsx("span", {})] }), draft.mappings.length === 0 && _jsx("div", { className: styles.empty, children: "No mappings configured." }), draft.mappings.map((m, i) => {
                        const match = suggestionByName.get((m.binary ?? "").toLowerCase());
                        const isElevated = match != null && !match.isAccessible;
                        return (_jsxs("div", { className: styles.mappingRow, children: [_jsx("select", { value: m.channel, onChange: (e) => updateMapping(i, { channel: e.target.value }), className: styles.cellSelect, children: AUDIO_CHANNELS.map((c) => _jsx("option", { value: c, children: c }, c)) }), _jsx("input", { value: m.binary, list: "audio-process-suggestions", onFocus: reloadSuggestions, onChange: (e) => {
                                        let v = e.target.value;
                                        if (v.endsWith(ELEVATED_SUFFIX))
                                            v = v.slice(0, -ELEVATED_SUFFIX.length);
                                        updateMapping(i, { binary: v });
                                    }, placeholder: "ProcessName", className: styles.cellInput }), _jsx("input", { value: m.device, onChange: (e) => updateMapping(i, { device: e.target.value }), placeholder: "(All)", className: styles.cellInput }), _jsx("input", { type: "checkbox", checked: m.useLatch, onChange: (e) => updateMapping(i, { useLatch: e.target.checked }) }), _jsx("input", { type: "checkbox", checked: m.onlyActive, onChange: (e) => updateMapping(i, { onlyActive: e.target.checked }) }), _jsx("span", { className: styles.statusCell, title: isElevated ? "Elevated — run Prosim2GSX as admin" : "", children: isElevated ? "Elevated — run Prosim2GSX as admin" : "" }), _jsx("button", { type: "button", className: styles.removeBtn, onClick: () => removeMapping(i), children: "\u00D7" })] }, i));
                    }), _jsx(PrimaryButton, { onClick: addMapping, variant: "secondary", children: "Add mapping" })] })), vmEnabled && (_jsxs(Section, { title: "VoiceMeeter Mappings", children: [_jsxs("div", { className: styles.activeAcpsRow, children: [_jsx("span", { className: styles.activeAcpsLabel, children: "Active ACPs (1\u20133)" }), ACP_SIDES_ORDERED.map((acp) => (_jsxs("label", { className: styles.acpCheckbox, children: [_jsx("input", { type: "checkbox", checked: activeAcps.includes(acp), onChange: (e) => toggleAcp(acp, e.target.checked) }), ACP_SIDE_LABELS[acp]] }, acp)))] }), vmConflict && (_jsxs("div", { className: styles.warningBanner, children: [_jsx("strong", { children: "Mapping conflict" }), " \u2014 Save disabled until resolved. ", vmConflict] })), draft.voiceMeeterFallbackReason && (_jsxs("div", { className: styles.warningBanner, children: [_jsx("strong", { children: "Fell back to ACP1 only at last bind" }), " \u2014 ", draft.voiceMeeterFallbackReason] })), _jsx("div", { className: styles.acpCards, children: ACP_SIDES_ORDERED.filter((acp) => activeAcps.includes(acp)).map((acp) => {
                            const list = acpMappings(draft, acp);
                            return (_jsxs("div", { className: styles.acpCard, children: [_jsx("div", { className: styles.acpCardTitle, children: ACP_SIDE_LABELS[acp] }), _jsxs("div", { className: styles.vmMappingsHeader, children: [_jsx("span", { children: "Channel" }), _jsx("span", { children: "Strip / Bus" }), _jsx("span", { children: "Latch" }), _jsx("span", {})] }), list.length === 0 && _jsx("div", { className: styles.empty, children: "No mappings on this ACP." }), list.map((m, i) => (_jsxs("div", { className: styles.vmMappingsRow, children: [_jsx("select", { value: m.channel, onChange: (e) => updateVmMapping(acp, i, { channel: e.target.value }), className: styles.cellSelect, children: AUDIO_CHANNELS.map((c) => _jsx("option", { value: c, children: c }, c)) }), _jsxs("select", { value: vmMappingKey(m), onChange: (e) => setVmMappingTargetFromKey(acp, i, e.target.value), disabled: strips.length === 0, className: styles.cellSelect, children: [strips.length === 0 && _jsx("option", { value: "", children: "(no strips available \u2014 load DLL or start VoiceMeeter)" }), strips.map((s) => (_jsx("option", { value: s.key, children: s.displayName }, s.key)))] }), _jsx("input", { type: "checkbox", checked: m.useLatch, onChange: (e) => updateVmMapping(acp, i, { useLatch: e.target.checked }) }), _jsx("button", { type: "button", className: styles.removeBtn, onClick: () => removeVmMapping(acp, i), children: "\u00D7" })] }, i))), _jsx(PrimaryButton, { onClick: () => addVmMapping(acp), variant: "secondary", children: "Add mapping" })] }, acp));
                        }) })] })), _jsx("datalist", { id: "audio-process-suggestions", children: suggestions.map((s) => (_jsx("option", { value: s.isAccessible ? s.processName : s.processName + ELEVATED_SUFFIX }, s.processName))) }), !vmEnabled && (_jsxs(Section, { title: "Device Blacklist", children: [draft.blacklist.length === 0 && _jsx("div", { className: styles.empty, children: "No devices blacklisted." }), draft.blacklist.map((d, i) => (_jsxs("div", { className: styles.blacklistRow, children: [_jsx("input", { value: d, onChange: (e) => updateBlacklist(i, e.target.value), placeholder: "Device name", className: styles.cellInput }), _jsx("button", { type: "button", className: styles.removeBtn, onClick: () => removeBlacklist(i), children: "\u00D7" })] }, i))), _jsx(PrimaryButton, { onClick: addBlacklist, variant: "secondary", children: "Add device" })] }))] }));
}
