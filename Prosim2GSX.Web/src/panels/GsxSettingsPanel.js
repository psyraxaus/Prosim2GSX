import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useEffect, useState } from "react";
import { useApi } from "../api/useApi";
import { Section } from "../components/forms/Section";
import { BoolField, NumberField, SelectField } from "../components/forms/Field";
import { PrimaryButton } from "../components/forms/PrimaryButton";
import { AUTO_DEICE_FLUID_OPTIONS, CONNECT_PCA_OPTIONS, PUSHBACK_TIMING_OPTIONS, REFUEL_METHOD_OPTIONS, REMOVE_STAIRS_OPTIONS, SERVICE_ACTIVATION_OPTIONS, SERVICE_CONSTRAINT_OPTIONS, SERVICE_TYPE_OPTIONS, TUG_OPTIONS, } from "../types";
import styles from "./GsxSettingsPanel.module.css";
export function GsxSettingsPanel() {
    const { get, post } = useApi();
    const [draft, setDraft] = useState(null);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState(null);
    const [info, setInfo] = useState(null);
    async function reload() {
        setError(null);
        try {
            const dto = await get("/gsxsettings");
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
        return _jsx("div", { className: styles.loading, children: error ? `Error: ${error}` : "Loading GSX settings…" });
    }
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
    return (_jsxs("div", { className: styles.panel, children: [_jsxs("div", { className: styles.toolbar, children: [_jsx(PrimaryButton, { onClick: save, disabled: saving, children: "Save" }), _jsx(PrimaryButton, { onClick: reload, variant: "secondary", disabled: saving, children: "Reload" }), _jsxs("div", { className: styles.toolbarStatus, children: [draft.profileName && _jsxs("span", { className: styles.profileName, children: ["Profile: ", draft.profileName] }), error && _jsx("span", { className: styles.error, children: error }), info && _jsx("span", { className: styles.info, children: info })] })] }), _jsxs(Section, { title: "Doors & Stairs", children: [_jsx(BoolField, { label: "Door Stair Handling", value: draft.doorStairHandling, onChange: (v) => update("doorStairHandling", v) }), _jsx(BoolField, { label: "Include L2 Door", value: draft.doorStairIncludeL2, onChange: (v) => update("doorStairIncludeL2", v) }), _jsx(BoolField, { label: "Door Cargo Handling", value: draft.doorCargoHandling, onChange: (v) => update("doorCargoHandling", v) }), _jsx(BoolField, { label: "Door Catering Handling", value: draft.doorCateringHandling, onChange: (v) => update("doorCateringHandling", v) }), _jsx(BoolField, { label: "Door Open on Boarding Active", value: draft.doorOpenBoardActive, onChange: (v) => update("doorOpenBoardActive", v) }), _jsx(BoolField, { label: "Cargo Doors Keep Open on Loaded", value: draft.doorsCargoKeepOpenOnLoaded, onChange: (v) => update("doorsCargoKeepOpenOnLoaded", v) }), _jsx(BoolField, { label: "Cargo Doors Keep Open on Unloaded", value: draft.doorsCargoKeepOpenOnUnloaded, onChange: (v) => update("doorsCargoKeepOpenOnUnloaded", v) }), _jsx(BoolField, { label: "Close Doors on Final", value: draft.closeDoorsOnFinal, onChange: (v) => update("closeDoorsOnFinal", v) })] }), _jsxs(Section, { title: "Jetway / Stairs", children: [_jsx(BoolField, { label: "Call on Preparation", value: draft.callJetwayStairsOnPrep, onChange: (v) => update("callJetwayStairsOnPrep", v) }), _jsx(BoolField, { label: "Call During Departure", value: draft.callJetwayStairsDuringDeparture, onChange: (v) => update("callJetwayStairsDuringDeparture", v) }), _jsx(BoolField, { label: "Call on Arrival", value: draft.callJetwayStairsOnArrival, onChange: (v) => update("callJetwayStairsOnArrival", v) }), _jsx(SelectField, { label: "Remove Stairs After Departure", value: draft.removeStairsAfterDepature, options: REMOVE_STAIRS_OPTIONS, onChange: (v) => update("removeStairsAfterDepature", v) }), _jsx(BoolField, { label: "Remove Jetway/Stairs on Final", value: draft.removeJetwayStairsOnFinal, onChange: (v) => update("removeJetwayStairsOnFinal", v) })] }), _jsxs(Section, { title: "Ground Equipment", children: [_jsx(BoolField, { label: "Place ProSim Stairs on Walkaround", value: draft.placeProsimStairsWalkaround, onChange: (v) => update("placeProsimStairsWalkaround", v) }), _jsx(BoolField, { label: "Clear Ground Equip on Beacon", value: draft.clearGroundEquipOnBeacon, onChange: (v) => update("clearGroundEquipOnBeacon", v) }), _jsx(BoolField, { label: "Gradual Ground Equip Removal", value: draft.gradualGroundEquipRemoval, onChange: (v) => update("gradualGroundEquipRemoval", v) }), _jsx(BoolField, { label: "Connect GPU with APU Running", value: draft.connectGpuWithApuRunning, onChange: (v) => update("connectGpuWithApuRunning", v) }), _jsx(SelectField, { label: "Connect PCA", value: draft.connectPca, options: CONNECT_PCA_OPTIONS, onChange: (v) => update("connectPca", v) }), _jsx(BoolField, { label: "PCA Override", value: draft.pcaOverride, onChange: (v) => update("pcaOverride", v) }), _jsx(NumberField, { label: "Chock Delay Min (s)", value: draft.chockDelayMin, onChange: (v) => update("chockDelayMin", v) }), _jsx(NumberField, { label: "Chock Delay Max (s)", value: draft.chockDelayMax, onChange: (v) => update("chockDelayMax", v) })] }), _jsxs(Section, { title: "Services", children: [_jsx(BoolField, { label: "Call Reposition", value: draft.callReposition, onChange: (v) => update("callReposition", v) }), _jsx(BoolField, { label: "Call Deboard on Arrival", value: draft.callDeboardOnArrival, onChange: (v) => update("callDeboardOnArrival", v) }), _jsx(BoolField, { label: "Run Departure During Deboarding", value: draft.runDepartureDuringDeboarding, onChange: (v) => update("runDepartureDuringDeboarding", v) }), _jsx(BoolField, { label: "Chime on Parked", value: draft.chimeOnParked, onChange: (v) => update("chimeOnParked", v) }), _jsx(BoolField, { label: "Chime on Deboard Complete", value: draft.chimeOnDeboardComplete, onChange: (v) => update("chimeOnDeboardComplete", v) })] }), _jsxs(Section, { title: "Departure Service Order", hint: "Order = activation sequence", children: [_jsxs("div", { className: styles.servicesHeader, children: [_jsx("span", { children: "Service" }), _jsx("span", { children: "Activation" }), _jsx("span", { children: "Constraint" }), _jsx("span", { children: "Min Flight" }), _jsx("span", {})] }), draft.departureServices.length === 0 && (_jsx("div", { className: styles.empty, children: "No departure services configured." })), draft.departureServices.map((s, idx) => (_jsxs("div", { className: styles.serviceRow, children: [_jsx("select", { value: s.serviceType, onChange: (e) => updateService(idx, { serviceType: e.target.value }), className: styles.cellSelect, children: SERVICE_TYPE_OPTIONS.map((o) => _jsx("option", { value: o.value, children: o.label }, o.value)) }), _jsx("select", { value: s.serviceActivation, onChange: (e) => updateService(idx, { serviceActivation: e.target.value }), className: styles.cellSelect, children: SERVICE_ACTIVATION_OPTIONS.map((o) => _jsx("option", { value: o.value, children: o.label }, o.value)) }), _jsx("select", { value: s.serviceConstraint, onChange: (e) => updateService(idx, { serviceConstraint: e.target.value }), className: styles.cellSelect, children: SERVICE_CONSTRAINT_OPTIONS.map((o) => _jsx("option", { value: o.value, children: o.label }, o.value)) }), _jsx("input", { type: "number", value: s.minimumFlightDuration, onChange: (e) => updateService(idx, { minimumFlightDuration: Number(e.target.value) }), className: styles.cellNumber, title: "Minimum flight duration (seconds)" }), _jsxs("div", { className: styles.serviceActions, children: [_jsx("button", { type: "button", className: styles.iconBtn, onClick: () => moveService(idx, -1), disabled: idx === 0, title: "Move up", children: "\u25B2" }), _jsx("button", { type: "button", className: styles.iconBtn, onClick: () => moveService(idx, 1), disabled: idx === draft.departureServices.length - 1, title: "Move down", children: "\u25BC" }), _jsx("button", { type: "button", className: styles.removeBtn, onClick: () => removeService(idx), title: "Remove", children: "\u00D7" })] })] }, idx))), _jsx(PrimaryButton, { onClick: addService, variant: "secondary", children: "Add service" })] }), _jsxs(Section, { title: "Refuel", children: [_jsx(SelectField, { label: "Refuel Method", value: draft.refuelMethod, options: REFUEL_METHOD_OPTIONS, onChange: (v) => update("refuelMethod", v) }), _jsx(NumberField, { label: "Refuel Rate (kg/s)", value: draft.refuelRateKgSec, step: 0.5, onChange: (v) => update("refuelRateKgSec", v) }), _jsx(NumberField, { label: "Target Refuel Time (s)", value: draft.refuelTimeTargetSeconds, onChange: (v) => update("refuelTimeTargetSeconds", v) }), _jsx(BoolField, { label: "Skip Fuel on Tankering", value: draft.skipFuelOnTankering, onChange: (v) => update("skipFuelOnTankering", v) }), _jsx(BoolField, { label: "Refuel Finish on Hose", value: draft.refuelFinishOnHose, onChange: (v) => update("refuelFinishOnHose", v) })] }), _jsxs(Section, { title: "Pushback", children: [_jsx(SelectField, { label: "Attach Tug During Boarding", value: draft.attachTugDuringBoarding, options: TUG_OPTIONS, onChange: (v) => update("attachTugDuringBoarding", v) }), _jsx(SelectField, { label: "Call Pushback When Tug Attached", value: draft.callPushbackWhenTugAttached, options: PUSHBACK_TIMING_OPTIONS, onChange: (v) => update("callPushbackWhenTugAttached", v) }), _jsx(BoolField, { label: "Call Pushback on Beacon", value: draft.callPushbackOnBeacon, onChange: (v) => update("callPushbackOnBeacon", v) })] }), _jsxs(Section, { title: "Beacon-Orchestrated Sequence", children: [_jsx(BoolField, { label: "Sequence on Beacon", value: draft.sequenceOnBeacon, onChange: (v) => update("sequenceOnBeacon", v) }), _jsx(NumberField, { label: "Doors Close Delay Min (s)", value: draft.seqDoorsCloseDelayMin, onChange: (v) => update("seqDoorsCloseDelayMin", v) }), _jsx(NumberField, { label: "Doors Close Delay Max (s)", value: draft.seqDoorsCloseDelayMax, onChange: (v) => update("seqDoorsCloseDelayMax", v) }), _jsx(NumberField, { label: "Jetway Retract Delay Min (s)", value: draft.seqJetwayRetractDelayMin, onChange: (v) => update("seqJetwayRetractDelayMin", v) }), _jsx(NumberField, { label: "Jetway Retract Delay Max (s)", value: draft.seqJetwayRetractDelayMax, onChange: (v) => update("seqJetwayRetractDelayMax", v) }), _jsx(NumberField, { label: "GPU Disconnect Delay Min (s)", value: draft.seqGpuDisconnectDelayMin, onChange: (v) => update("seqGpuDisconnectDelayMin", v) }), _jsx(NumberField, { label: "GPU Disconnect Delay Max (s)", value: draft.seqGpuDisconnectDelayMax, onChange: (v) => update("seqGpuDisconnectDelayMax", v) })] }), _jsxs(Section, { title: "Operator", children: [_jsx(BoolField, { label: "Operator Auto-Select", value: draft.operatorAutoSelect, onChange: (v) => update("operatorAutoSelect", v) }), _jsx(ListEditor, { label: "Operator Preferences", values: draft.operatorPreferences, onChangeAt: (i, v) => updateListItem("operatorPreferences", i, v), onAdd: () => addListItem("operatorPreferences"), onRemove: (i) => removeListItem("operatorPreferences", i) })] }), _jsx(Section, { title: "Company Hubs", children: _jsx(ListEditor, { label: "ICAO Hubs", values: draft.companyHubs, onChangeAt: (i, v) => updateListItem("companyHubs", i, v), onAdd: () => addListItem("companyHubs"), onRemove: (i) => removeListItem("companyHubs", i) }) }), _jsxs(Section, { title: "Skip Questions", children: [_jsx(BoolField, { label: "Skip Walkaround", value: draft.skipWalkAround, onChange: (v) => update("skipWalkAround", v) }), _jsx(BoolField, { label: "Skip Crew Question", value: draft.skipCrewQuestion, onChange: (v) => update("skipCrewQuestion", v) }), _jsx(BoolField, { label: "Skip Follow Me", value: draft.skipFollowMe, onChange: (v) => update("skipFollowMe", v) }), _jsx(BoolField, { label: "Keep Direction Menu Open", value: draft.keepDirectionMenuOpen, onChange: (v) => update("keepDirectionMenuOpen", v) }), _jsx(BoolField, { label: "Answer Cabin Call (Ground)", value: draft.answerCabinCallGround, onChange: (v) => update("answerCabinCallGround", v) }), _jsx(NumberField, { label: "Cabin Call Delay (Ground, ms)", value: draft.delayCabinCallGround, onChange: (v) => update("delayCabinCallGround", v) }), _jsx(BoolField, { label: "Answer Cabin Call (Air)", value: draft.answerCabinCallAir, onChange: (v) => update("answerCabinCallAir", v) }), _jsx(NumberField, { label: "Cabin Call Delay (Air, ms)", value: draft.delayCabinCallAir, onChange: (v) => update("delayCabinCallAir", v) })] }), _jsxs(Section, { title: "Aircraft / OFP", children: [_jsx(NumberField, { label: "Final Delay Min (s)", value: draft.finalDelayMin, onChange: (v) => update("finalDelayMin", v) }), _jsx(NumberField, { label: "Final Delay Max (s)", value: draft.finalDelayMax, onChange: (v) => update("finalDelayMax", v) }), _jsx(BoolField, { label: "Save / Load FOB", value: draft.fuelSaveLoadFob, onChange: (v) => update("fuelSaveLoadFob", v) }), _jsx(BoolField, { label: "Randomize Pax", value: draft.randomizePax, onChange: (v) => update("randomizePax", v) }), _jsx(NumberField, { label: "Chance Per Seat", value: draft.chancePerSeat, step: 0.001, onChange: (v) => update("chancePerSeat", v) })] }), _jsxs(Section, { title: "Auto-Deice", children: [_jsx(BoolField, { label: "Auto-Deice Enabled", value: draft.autoDeiceEnabled, onChange: (v) => update("autoDeiceEnabled", v) }), _jsx(SelectField, { label: "Deice Fluid", value: draft.autoDeiceFluid, options: AUTO_DEICE_FLUID_OPTIONS, onChange: (v) => update("autoDeiceFluid", v) })] })] }));
}
function ListEditor({ label, values, onChangeAt, onAdd, onRemove }) {
    return (_jsxs("div", { className: styles.listEditor, children: [_jsx("div", { className: styles.listLabel, children: label }), values.length === 0 && _jsx("div", { className: styles.empty, children: "No entries." }), values.map((v, i) => (_jsxs("div", { className: styles.listRow, children: [_jsx("input", { value: v, onChange: (e) => onChangeAt(i, e.target.value), className: styles.cellInput }), _jsx("button", { type: "button", className: styles.removeBtn, onClick: () => onRemove(i), children: "\u00D7" })] }, i))), _jsx(PrimaryButton, { onClick: onAdd, variant: "secondary", children: "Add" })] }));
}
