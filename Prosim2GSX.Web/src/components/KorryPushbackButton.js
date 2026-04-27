import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import styles from "./KorryPushbackButton.module.css";
// Cockpit Korry pushbutton mirrored from WPF Prosim2GSX.UI.KorryPushbackButton.
//
// Layout (90×82 + subtitle below):
//   - Outer bevelled body, rounded 5px, 2px border (lit when active).
//   - Upper icon zone (54px) with SVG arrow — green for tail directions,
//     amber for straight (matches the WPF auto/manual colour scheme).
//   - 1px separator + lower legend strip showing the label, dim when off
//     and lit blue (tail) / amber (straight) when on.
//   - Subtitle text below the body (e.g. "NOSE RIGHT").
//
// Path coordinates and colour values copied verbatim from
// KorryPushbackButton.xaml so the rendering is pixel-equivalent.
export function KorryPushbackButton({ arrow, label, subtitle, isActive, onClick, disabled, title, }) {
    const tone = arrow === "straight" ? "amber" : "green";
    const bodyClasses = [
        styles.body,
        tone === "green" ? styles.green : styles.amber,
        isActive ? styles.active : "",
    ].filter(Boolean).join(" ");
    return (_jsxs("div", { className: styles.wrap, children: [_jsxs("button", { type: "button", title: title, "aria-pressed": isActive, disabled: disabled, className: bodyClasses, onClick: onClick, children: [_jsx("div", { className: styles.iconZone, children: renderArrow(arrow) }), _jsx("div", { className: styles.separator }), _jsx("div", { className: styles.legendStrip, children: _jsx("span", { className: styles.legendText, children: label }) })] }), _jsx("div", { className: styles.subtitle, children: subtitle })] }));
}
function renderArrow(arrow) {
    // 56×44 viewBox matches the WPF Canvas dimensions; using SVG instead
    // of WPF Path/Line/Polygon but the same coordinate system.
    switch (arrow) {
        case "tailLeft":
            return (_jsxs("svg", { viewBox: "0 0 56 44", className: styles.arrow, "aria-hidden": "true", children: [_jsx("path", { d: "M 42 3 L 42 21 Q 42 37 28 37 L 20 37", className: styles.arrowStroke, strokeWidth: "4", strokeLinecap: "round", fill: "none" }), _jsx("polygon", { points: "7,37 19,30 19,44", className: styles.arrowFill })] }));
        case "straight":
            return (_jsxs("svg", { viewBox: "0 0 56 44", className: styles.arrow, "aria-hidden": "true", children: [_jsx("line", { x1: "28", y1: "3", x2: "28", y2: "34", className: styles.arrowStroke, strokeWidth: "4", strokeLinecap: "round" }), _jsx("polygon", { points: "28,43 18,30 38,30", className: styles.arrowFill })] }));
        case "tailRight":
            return (_jsxs("svg", { viewBox: "0 0 56 44", className: styles.arrow, "aria-hidden": "true", children: [_jsx("path", { d: "M 14 3 L 14 21 Q 14 37 28 37 L 36 37", className: styles.arrowStroke, strokeWidth: "4", strokeLinecap: "round", fill: "none" }), _jsx("polygon", { points: "49,37 37,30 37,44", className: styles.arrowFill })] }));
    }
}
