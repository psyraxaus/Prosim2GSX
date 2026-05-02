import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useEffect, useMemo, useState } from "react";
import { useApi } from "../api/useApi";
import { useAppState } from "../state/AppStateContext";
import styles from "./ChecklistsPanel.module.css";
// ECAM-style Checklists tab. Mirrors the WPF ModelChecklist surface:
// dropdown for the checklist set (saved to AircraftProfile.ChecklistName),
// dropdown for the current section, ECAM-style item list with green
// checked / cyan pending / current-item border highlight, RESET / C/L
// MENU / MSG LIST / C/L COMPLETE footer buttons.
//
// All state mutations go through POST /api/checklists/* endpoints; the
// server fans out the resulting whole-snapshot via WS so multiple
// clients (e.g. WPF + browser) stay in lockstep.
export function ChecklistsPanel() {
    const { get, post } = useApi();
    const { state, dispatch } = useAppState();
    const [error, setError] = useState(null);
    async function load() {
        setError(null);
        try {
            const dto = await get("/checklists");
            dispatch({ type: "set", channel: "checklists", state: dto });
        }
        catch (e) {
            setError(e.message ?? "Failed to load");
        }
    }
    useEffect(() => {
        load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);
    const dto = state.checklists;
    const currentSection = useMemo(() => {
        if (!dto?.sections)
            return null;
        if (dto.currentSectionIndex < 0 || dto.currentSectionIndex >= dto.sections.length)
            return null;
        return dto.sections[dto.currentSectionIndex];
    }, [dto]);
    async function selectChecklist(name) {
        if (!name || name === dto?.currentChecklistName)
            return;
        setError(null);
        try {
            const body = { name };
            await post("/checklists/select", body);
        }
        catch (e) {
            setError(e.message ?? "Failed to select checklist");
        }
    }
    async function selectSection(idx) {
        if (idx === dto?.currentSectionIndex)
            return;
        setError(null);
        try {
            const body = { sectionIndex: idx };
            await post("/checklists/select-section", body);
        }
        catch (e) {
            setError(e.message ?? "Failed to change section");
        }
    }
    async function toggleItem(itemIndex) {
        if (dto == null)
            return;
        setError(null);
        try {
            const body = {
                sectionIndex: dto.currentSectionIndex,
                itemIndex,
            };
            await post("/checklists/toggle", body);
        }
        catch (e) {
            setError(e.message ?? "Failed to toggle item");
        }
    }
    async function resetSection() {
        if (dto == null)
            return;
        setError(null);
        try {
            const body = { sectionIndex: dto.currentSectionIndex };
            await post("/checklists/reset-section", body);
        }
        catch (e) {
            setError(e.message ?? "Failed to reset section");
        }
    }
    async function complete() {
        setError(null);
        try {
            await post("/checklists/complete", {});
        }
        catch (e) {
            setError(e.message ?? "Failed to complete section");
        }
    }
    if (!dto) {
        return (_jsx("div", { className: styles.panel, children: _jsx("div", { className: styles.error, children: error ? `Error: ${error}` : "Loading checklist…" }) }));
    }
    return (_jsxs("div", { className: styles.panel, children: [_jsxs("div", { className: styles.headerBar, children: [_jsxs("div", { className: styles.row, children: [_jsx("span", { className: styles.label, children: "C/L" }), _jsxs("select", { className: styles.select, value: dto.currentChecklistName, onChange: (e) => selectChecklist(e.target.value), children: [dto.availableChecklists.length === 0 && _jsx("option", { value: "", children: "(none available)" }), dto.availableChecklists.map((n) => (_jsx("option", { value: n, children: n }, n)))] })] }), _jsx("div", { className: styles.row, children: _jsx("select", { className: styles.select, value: dto.currentSectionIndex, onChange: (e) => selectSection(parseInt(e.target.value, 10)), children: dto.sections.map((s, i) => (_jsx("option", { value: i, children: s.title }, i))) }) })] }), _jsxs("div", { className: styles.body, children: [currentSection == null && _jsx("div", { className: styles.error, children: "No section loaded." }), currentSection?.items?.map((it, i) => (_jsx(ChecklistRow, { item: it, isCurrent: i === dto.currentItemIndex, onClick: () => toggleItem(i) }, i)))] }), error && _jsx("div", { className: styles.error, children: error }), _jsxs("div", { className: styles.footer, children: [_jsx("button", { type: "button", className: styles.button, onClick: resetSection, children: "RESET" }), _jsx("button", { type: "button", className: styles.button, onClick: () => {
                            const sel = document.querySelector(`.${styles.headerBar} select:nth-of-type(1)`);
                            sel?.focus();
                        }, children: "C/L MENU" }), _jsx("button", { type: "button", className: styles.button, disabled: true, children: "MSG LIST" }), _jsx("button", { type: "button", className: styles.button, onClick: complete, children: "C/L COMPLETE" })] })] }));
}
function ChecklistRow({ item, isCurrent, onClick }) {
    if (item.isSeparator) {
        return _jsx("hr", { className: styles.separator });
    }
    if (item.isNote) {
        return _jsx("div", { className: styles.note, children: item.label });
    }
    if (item.isChecked) {
        return (_jsxs("div", { className: `${styles.itemRow} ${styles.checked}`, children: [_jsx("span", { className: styles.itemCheck, children: "\u2713" }), _jsx("span", { children: item.label }), _jsx("span", { className: styles.itemDots, children: "...................................................................................................." }), _jsx("span", { children: item.value })] }));
    }
    if (isCurrent) {
        return (_jsxs("div", { className: `${styles.itemRow} ${styles.pending} ${styles.current} ${item.isManual ? styles.clickable : ""}`, onClick: item.isManual ? onClick : undefined, role: item.isManual ? "button" : undefined, children: [_jsx("span", { className: styles.itemCheck, children: "\u25A1" }), _jsx("span", { children: item.label }), _jsx("span", { className: styles.itemDots, children: "...................................................................................................." }), _jsx("span", { children: item.value })] }));
    }
    return (_jsxs("div", { className: `${styles.itemRow} ${styles.pending}`, children: [_jsx("span", {}), _jsx("span", { children: item.label }), _jsx("span", { className: styles.itemDots, children: "...................................................................................................." }), _jsx("span", { children: item.value })] }));
}
