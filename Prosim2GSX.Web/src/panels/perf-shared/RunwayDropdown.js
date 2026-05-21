import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import styles from "./PerfShared.module.css";
export function RunwayDropdown({ runways, runwayId, intersectionName = "", withIntersections = false, onRunwayChange, onIntersectionChange, disabled, }) {
    const current = runways.find((r) => r.runwayId === runwayId);
    const hasIntersections = withIntersections && (current?.intersections.length ?? 0) > 0;
    return (_jsxs("div", { className: styles.runwayDropdown, children: [_jsxs("select", { value: runwayId, disabled: disabled || runways.length === 0, onChange: (e) => onRunwayChange(e.target.value), "aria-label": "Runway", children: [runways.length === 0 && _jsx("option", { value: "", children: "\u2014" }), runways.map((r) => (_jsxs("option", { value: r.runwayId, children: ["RWY ", r.runwayId, r.lengthFt > 0 ? ` · ${r.lengthFt} ft` : ""] }, r.runwayId)))] }), hasIntersections && (_jsxs("select", { value: intersectionName, disabled: disabled, onChange: (e) => onIntersectionChange?.(e.target.value), "aria-label": "Intersection", children: [_jsx("option", { value: "", children: "Full length" }), current?.intersections.map((i) => (_jsxs("option", { value: i.name, children: [i.name, " \u00B7 ", i.toraFt, " ft"] }, i.name)))] }))] }));
}
