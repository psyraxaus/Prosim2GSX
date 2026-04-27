import { jsx as _jsx, jsxs as _jsxs, Fragment as _Fragment } from "react/jsx-runtime";
import { useEffect, useState } from "react";
import { useApi } from "../api/useApi";
import { Section } from "../components/forms/Section";
import { PrimaryButton } from "../components/forms/PrimaryButton";
import { PROFILE_MATCH_TYPE_OPTIONS, } from "../types";
import styles from "./AircraftProfilesPanel.module.css";
// CRUD UI for Config.AircraftProfiles. Mirrors the WPF Profiles tab:
// "Current Aircraft" context section above + per-profile cards. Uses
// the Phase 8.0b CommandRegistry-backed REST endpoints; each command's
// response carries the full updated list, so the panel re-renders from
// that without a separate refetch round-trip.
//
// Editing is per-card: clicking Edit replaces the read-only fields with
// inputs + Save / Cancel. On Save, if the Name changed the panel
// dispatches profiles.rename first, then profiles.updateMetadata for any
// MatchType / MatchString diff. The default profile is editable only by
// MatchString — its Name is locked (Config fallback path references it
// by string) and MatchType must stay Default (server enforces both).
export function AircraftProfilesPanel() {
    const { get, post } = useApi();
    const [list, setList] = useState(null);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState(null);
    const [info, setInfo] = useState(null);
    // Per-card edit draft. Single editing card at a time keeps state simple.
    const [editingName, setEditingName] = useState(null);
    const [draftName, setDraftName] = useState("");
    const [draftMatchType, setDraftMatchType] = useState("Default");
    const [draftMatchString, setDraftMatchString] = useState("");
    async function reload() {
        setError(null);
        try {
            const dto = await get("/profiles");
            setList(dto);
        }
        catch (e) {
            setError(e.message ?? "Failed to load");
        }
    }
    useEffect(() => { reload(); }, []);
    function flashInfo(message) {
        setInfo(message);
        setTimeout(() => setInfo(null), 1500);
    }
    function startEdit(p) {
        setEditingName(p.name);
        setDraftName(p.name);
        setDraftMatchType(p.matchType);
        setDraftMatchString(p.matchString);
        setError(null);
        setInfo(null);
    }
    function cancelEdit() {
        setEditingName(null);
        setError(null);
    }
    async function saveEdit(p) {
        setBusy(true);
        setError(null);
        setInfo(null);
        try {
            let working = p.name;
            const trimmedName = draftName.trim();
            // Rename first (if changed and non-empty) so the metadata update can
            // address the new name. Server enforces guards (default rename
            // forbidden, duplicates rejected) and surfaces them as 400s.
            if (trimmedName && trimmedName !== p.name) {
                const renamed = await post("/profiles/rename", {
                    oldName: p.name, newName: trimmedName,
                });
                setList(renamed);
                working = trimmedName;
            }
            // Update metadata if anything else changed.
            if (draftMatchType !== p.matchType || (draftMatchString ?? "") !== (p.matchString ?? "")) {
                const updated = await post("/profiles/update-metadata", {
                    name: working, matchType: draftMatchType, matchString: draftMatchString,
                });
                setList(updated);
            }
            setEditingName(null);
            flashInfo("Saved.");
        }
        catch (e) {
            setError(e.message ?? "Save failed");
        }
        finally {
            setBusy(false);
        }
    }
    async function setActive(p) {
        if (p.isActive)
            return;
        setBusy(true);
        setError(null);
        setInfo(null);
        try {
            const dto = await post("/profiles/set-active", {
                name: p.name,
            });
            setList(dto);
            flashInfo(`Active profile: ${p.name}`);
        }
        catch (e) {
            setError(e.message ?? "Set active failed");
        }
        finally {
            setBusy(false);
        }
    }
    async function cloneProfile(p) {
        const suggested = `Clone of ${p.name}`;
        const newName = window.prompt(`Clone "${p.name}" as:`, suggested);
        if (!newName?.trim())
            return;
        setBusy(true);
        setError(null);
        setInfo(null);
        try {
            const dto = await post("/profiles/clone", {
                sourceName: p.name, newName: newName.trim(),
            });
            setList(dto);
            flashInfo(`Cloned to "${newName.trim()}".`);
        }
        catch (e) {
            setError(e.message ?? "Clone failed");
        }
        finally {
            setBusy(false);
        }
    }
    async function deleteProfile(p) {
        if (!window.confirm(`Delete profile "${p.name}"? This cannot be undone.`))
            return;
        setBusy(true);
        setError(null);
        setInfo(null);
        try {
            const dto = await post("/profiles/delete", {
                name: p.name,
            });
            setList(dto);
            flashInfo(`Deleted "${p.name}".`);
        }
        catch (e) {
            setError(e.message ?? "Delete failed");
        }
        finally {
            setBusy(false);
        }
    }
    if (!list) {
        return (_jsx("div", { className: styles.loading, children: error ? `Error: ${error}` : "Loading aircraft profiles…" }));
    }
    const hasAircraftInfo = !!list.currentAirline || !!list.currentTitle || !!list.currentRegistration;
    return (_jsxs("div", { className: styles.panel, children: [_jsxs("div", { className: styles.toolbar, children: [_jsxs("span", { className: styles.activeName, children: ["Active: ", _jsx("strong", { children: list.activeName || "—" })] }), _jsxs("div", { className: styles.toolbarStatus, children: [error && _jsx("span", { className: styles.error, children: error }), info && _jsx("span", { className: styles.info, children: info })] })] }), _jsxs(Section, { title: "Current Aircraft", hint: hasAircraftInfo ? "" : "Sim not connected — profile matching will use defaults", children: [_jsxs("div", { className: styles.kv, children: [_jsx("span", { className: styles.kvLabel, children: "Airline" }), _jsx("span", { className: styles.kvValue, children: list.currentAirline || "—" })] }), _jsxs("div", { className: styles.kv, children: [_jsx("span", { className: styles.kvLabel, children: "Registration" }), _jsx("span", { className: styles.kvValue, children: list.currentRegistration || "—" })] }), _jsxs("div", { className: styles.kv, children: [_jsx("span", { className: styles.kvLabel, children: "Title / Livery" }), _jsx("span", { className: styles.kvValue, children: list.currentTitle || "—" })] }), _jsxs("div", { className: styles.kv, children: [_jsx("span", { className: styles.kvLabel, children: "Active Profile" }), _jsx("span", { className: styles.kvValue, children: _jsx("strong", { children: list.activeName || "—" }) })] })] }), _jsxs(Section, { title: "Profiles", hint: "Profiles are matched in this order: Registration \u2192 Title \u2192 Airline \u2192 Default fallback", children: [list.profiles.length === 0 && (_jsx("div", { className: styles.empty, children: "No profiles configured." })), _jsx("div", { className: styles.profileList, children: list.profiles.map((p) => {
                            const isEditing = editingName === p.name;
                            // Default profile: name is locked and MatchType must stay Default
                            // (server-enforced). Match string is still editable.
                            const nameLocked = p.isDefault;
                            const typeLocked = p.isDefault;
                            return (_jsxs("div", { className: `${styles.card} ${p.isActive ? styles.cardActive : ""}`, children: [_jsxs("div", { className: styles.cardHeader, children: [isEditing ? (_jsx("input", { type: "text", value: draftName, onChange: (e) => setDraftName(e.target.value), disabled: nameLocked || busy, className: styles.nameInput, placeholder: "Profile name", spellCheck: false })) : (_jsx("span", { className: styles.name, children: p.name })), _jsxs("div", { className: styles.badges, children: [p.isActive && _jsx("span", { className: `${styles.badge} ${styles.badgeActive}`, children: "ACTIVE" }), p.isDefault && _jsx("span", { className: `${styles.badge} ${styles.badgeDefault}`, children: "DEFAULT" })] })] }), _jsxs("div", { className: styles.cardBody, children: [_jsxs("div", { className: styles.field, children: [_jsx("label", { className: styles.fieldLabel, children: "Match Type" }), isEditing ? (_jsx("select", { value: draftMatchType, onChange: (e) => setDraftMatchType(e.target.value), disabled: typeLocked || busy, className: styles.fieldInput, children: PROFILE_MATCH_TYPE_OPTIONS.map((o) => (_jsx("option", { value: o.value, children: o.label }, o.value))) })) : (_jsx("span", { className: styles.fieldValue, children: p.matchType }))] }), _jsxs("div", { className: styles.field, children: [_jsx("label", { className: styles.fieldLabel, children: "Match String" }), isEditing ? (_jsx("input", { type: "text", value: draftMatchString, onChange: (e) => setDraftMatchString(e.target.value), disabled: busy, className: styles.fieldInput, placeholder: "ICAO airline / aircraft title / registration", spellCheck: false })) : (_jsx("span", { className: styles.fieldValueMono, children: p.matchString || "—" }))] })] }), _jsx("div", { className: styles.cardActions, children: isEditing ? (_jsxs(_Fragment, { children: [_jsx(PrimaryButton, { onClick: () => saveEdit(p), disabled: busy, children: busy ? "Saving…" : "Save" }), _jsx(PrimaryButton, { onClick: cancelEdit, variant: "secondary", disabled: busy, children: "Cancel" })] })) : (_jsxs(_Fragment, { children: [!p.isActive && (_jsx(PrimaryButton, { onClick: () => setActive(p), disabled: busy, children: "Set Active" })), _jsx(PrimaryButton, { onClick: () => startEdit(p), variant: "secondary", disabled: busy, children: "Edit" }), _jsx(PrimaryButton, { onClick: () => cloneProfile(p), variant: "secondary", disabled: busy, children: "Clone" }), !p.isDefault && !p.isActive && (_jsx(PrimaryButton, { onClick: () => deleteProfile(p), variant: "danger", disabled: busy, children: "Delete" }))] })) })] }, p.name));
                        }) })] })] }));
}
