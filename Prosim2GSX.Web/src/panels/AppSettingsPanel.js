import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useEffect, useState } from "react";
import { useApi } from "../api/useApi";
import { storeToken } from "../auth/auth";
import { Section } from "../components/forms/Section";
import { BoolField, NumberField, SelectField, TextField } from "../components/forms/Field";
import { PrimaryButton } from "../components/forms/PrimaryButton";
import { DISPLAY_UNIT_OPTIONS, DISPLAY_UNIT_SOURCE_OPTIONS, } from "../types";
import styles from "./AppSettingsPanel.module.css";
// REST GET on mount → local draft state. POST replaces draft on save.
// WS updates intentionally don't auto-rebase the draft (would clobber
// the user's mid-edit). User can press Reload to refetch.
export function AppSettingsPanel() {
    const { get, post } = useApi();
    const [draft, setDraft] = useState(null);
    const [themes, setThemes] = useState([]);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState(null);
    const [info, setInfo] = useState(null);
    async function reload() {
        setError(null);
        try {
            const [dto, t] = await Promise.all([
                get("/appsettings"),
                get("/appsettings/themes"),
            ]);
            setDraft(dto);
            setThemes(t);
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
            const fresh = await post("/appsettings", draft);
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
    async function regenerateToken() {
        setError(null);
        setInfo(null);
        try {
            const res = await post("/appsettings/regenerate-token");
            storeToken(res.token);
            setInfo("New token stored. WebSocket will reconnect with the new token.");
            // The server already kicked existing WS connections via TokenRotated;
            // the React WS hook will see close 1008 and bounce to the auth gate
            // unless we proactively put the new token in localStorage first
            // (which we just did) — then reconnect resolves cleanly via the
            // current React session.
            await reload();
        }
        catch (e) {
            setError(e.message ?? "Regenerate failed");
        }
    }
    if (!draft) {
        return _jsx("div", { className: styles.loading, children: error ? `Error: ${error}` : "Loading settings…" });
    }
    function update(key, value) {
        setDraft((d) => (d ? { ...d, [key]: value } : d));
    }
    const themeOptions = themes.map((t) => ({ value: t, label: t }));
    return (_jsxs("div", { className: styles.panel, children: [_jsxs("div", { className: styles.toolbar, children: [_jsx(PrimaryButton, { onClick: save, disabled: saving, children: "Save" }), _jsx(PrimaryButton, { onClick: reload, variant: "secondary", disabled: saving, children: "Reload" }), _jsxs("div", { className: styles.toolbarStatus, children: [error && _jsx("span", { className: styles.error, children: error }), info && _jsx("span", { className: styles.info, children: info })] })] }), _jsx(Section, { title: "Theme", children: _jsx(SelectField, { label: "Theme", value: draft.currentTheme, options: themeOptions.length ? themeOptions : [{ value: draft.currentTheme, label: draft.currentTheme }], onChange: (v) => update("currentTheme", v) }) }), _jsxs(Section, { title: "Display", children: [_jsx(SelectField, { label: "UI Unit Source", value: draft.displayUnitSource, options: DISPLAY_UNIT_SOURCE_OPTIONS, onChange: (v) => update("displayUnitSource", v) }), _jsx(SelectField, { label: "UI Default Unit", value: draft.displayUnitDefault, options: DISPLAY_UNIT_OPTIONS, onChange: (v) => update("displayUnitDefault", v) }), _jsx(TextField, { label: "UI Current Unit", value: draft.displayUnitCurrent, readOnly: true, onChange: () => { } }), _jsx(BoolField, { label: "Open UI on Start", value: draft.openAppWindowOnStart, onChange: (v) => update("openAppWindowOnStart", v) }), _jsx(BoolField, { label: "Solari Animation", value: draft.solariAnimationEnabled, onChange: (v) => update("solariAnimationEnabled", v) })] }), _jsxs(Section, { title: "Fuel & Weight", hint: "Values stored in kg; UI shows converted unit", children: [_jsx(NumberField, { label: "ProSim Bag Weight (kg)", value: draft.prosimWeightBag, onChange: (v) => update("prosimWeightBag", v) }), _jsx(NumberField, { label: "FOB Reset Default (kg)", value: draft.fuelResetDefaultKg, onChange: (v) => update("fuelResetDefaultKg", v) }), _jsx(NumberField, { label: "Fuel Compare Variance (kg)", value: draft.fuelCompareVariance, onChange: (v) => update("fuelCompareVariance", v) }), _jsx(BoolField, { label: "Round Fuel to 100s", value: draft.fuelRoundUp100, onChange: (v) => update("fuelRoundUp100", v) })] }), _jsxs(Section, { title: "Audio Cues", children: [_jsx(BoolField, { label: "Ding on Startup", value: draft.dingOnStartup, onChange: (v) => update("dingOnStartup", v) }), _jsx(BoolField, { label: "Ding on Final LS", value: draft.dingOnFinal, onChange: (v) => update("dingOnFinal", v) })] }), _jsxs(Section, { title: "Cargo & Doors", children: [_jsx(NumberField, { label: "Cargo Change Rate", suffix: "% / s", value: draft.cargoPercentChangePerSec, onChange: (v) => update("cargoPercentChangePerSec", v) }), _jsx(NumberField, { label: "Cargo Door Open Delay", suffix: "s", value: draft.doorCargoOpenDelay, onChange: (v) => update("doorCargoOpenDelay", v) }), _jsx(NumberField, { label: "Cargo Door Close Delay", suffix: "s", value: draft.doorCargoDelay, onChange: (v) => update("doorCargoDelay", v) })] }), _jsxs(Section, { title: "GSX Behaviour", children: [_jsx(BoolField, { label: "Reset GSX Vars in Flight", value: draft.resetGsxStateVarsFlight, onChange: (v) => update("resetGsxStateVarsFlight", v) }), _jsx(BoolField, { label: "Restart GSX on Taxi-In", value: draft.restartGsxOnTaxiIn, onChange: (v) => update("restartGsxOnTaxiIn", v) }), _jsx(BoolField, { label: "Restart GSX on Startup Fail", value: draft.restartGsxStartupFail, onChange: (v) => update("restartGsxStartupFail", v) }), _jsx(NumberField, { label: "Max Startup Failures", value: draft.gsxMenuStartupMaxFail, onChange: (v) => update("gsxMenuStartupMaxFail", v) }), _jsx(BoolField, { label: "Run GSX Service", value: draft.runGsxService, onChange: (v) => update("runGsxService", v) }), _jsx(BoolField, { label: "Run Audio Service", value: draft.runAudioService, onChange: (v) => update("runAudioService", v) }), _jsx(BoolField, { label: "Use SayIntentions", value: draft.useSayIntentions, onChange: (v) => update("useSayIntentions", v) })] }), _jsx(Section, { title: "ProSim SDK", children: _jsx(TextField, { label: "ProSim SDK Path", value: draft.proSimSdkPath, monospace: true, onChange: (v) => update("proSimSdkPath", v) }) }), _jsxs(Section, { title: "Web Interface", hint: "Hot-toggle on save", children: [_jsx(BoolField, { label: "Enable Web Server", value: draft.webServerEnabled, onChange: (v) => update("webServerEnabled", v) }), _jsx(NumberField, { label: "Port", value: draft.webServerPort, onChange: (v) => update("webServerPort", v) }), _jsx(BoolField, { label: "Expose to LAN", value: draft.webServerBindAll, onChange: (v) => update("webServerBindAll", v) }), _jsx(TextField, { label: "Auth Token", value: draft.webServerAuthToken, readOnly: true, monospace: true, onChange: () => { } }), _jsxs("div", { className: styles.tokenActions, children: [_jsx(PrimaryButton, { onClick: regenerateToken, variant: "danger", children: "Regenerate Token" }), _jsx("span", { className: styles.hint, children: "Existing clients (including this browser) reconnect with the new token." })] })] })] }));
}
