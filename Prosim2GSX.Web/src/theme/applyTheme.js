// Maps WPF theme JSON colour tokens onto the CSS custom properties the
// React components consume. The status colours (success / warning /
// danger) intentionally stay fixed in src/styles/theme.css — they convey
// state, not theme.
//
// Beyond the raw JSON tokens, this function DERIVES three sets of values
// that the WPF ThemeManager.ApplyThemeColorsToResources also derives:
//
//   1. Input / button surface variants (`--bg-input`, `--bg-button`)
//      shifted from `sectionBackground` based on whether the theme is
//      "dark" (avg RGB < 128). Without these, anything sitting on
//      `--bg-secondary` (the always-dark tab-bar colour) ends up
//      dark-on-dark in Light/Qantas/Lufthansa themes.
//
//   2. Border colours picked for contrast against the section card
//      surface — translucent white on dark themes, translucent black
//      on light ones, so a single border value works everywhere.
//
//   3. Text variants. `--text-primary/secondary/muted` ride on cards
//      (sectionBackground), so they use `contentText` directly or
//      mixed toward the card. `--text-on-dark/-muted` are for elements
//      that always sit on dark bars regardless of theme (TabBar,
//      DirtyBar, headers) — bound to `headerText` which the JSON keeps
//      white across every shipped theme.
export function applyTheme(colors) {
    const root = document.documentElement;
    const set = (name, value) => root.style.setProperty(name, value);
    // Detect dark vs light surface so we know which direction to shift.
    const sectionBg = parseHex(colors.sectionBackground);
    const sectionBgBrightness = sectionBg
        ? (sectionBg.r + sectionBg.g + sectionBg.b) / 3
        : 128;
    const isDark = sectionBgBrightness < 128;
    // Background surfaces — direct mapping from the JSON tokens.
    set("--bg-primary", colors.contentBackground);
    set("--bg-secondary", colors.tabBarBackground);
    set("--bg-card", colors.sectionBackground);
    set("--bg-card-hover", shiftHex(colors.sectionBackground, isDark ? 10 : -8));
    // Derived input / button surfaces. Mirrors the WPF ThemeManager.Shift
    // logic (lighter for dark themes, slightly darker for light themes)
    // so an input or button always reads as a raised surface against the
    // card behind it.
    set("--bg-input", shiftHex(colors.sectionBackground, isDark ? 22 : -15));
    set("--bg-button", shiftHex(colors.sectionBackground, isDark ? 12 : -28));
    // Borders — alpha overlays of black/white pick the right contrast for
    // both polarities without needing extra theme tokens.
    set("--border", isDark ? "rgba(255,255,255,0.10)" : "rgba(0,0,0,0.12)");
    set("--border-strong", isDark ? "rgba(255,255,255,0.22)" : "rgba(0,0,0,0.22)");
    // Text that rides on a card (most panel content).
    set("--text-primary", colors.contentText);
    set("--text-secondary", mixHex(colors.contentText, colors.sectionBackground, 0.3));
    set("--text-muted", mixHex(colors.contentText, colors.sectionBackground, 0.55));
    // Text that ALWAYS rides on a dark bar — TabBar (tabBarBackground),
    // DirtyBar, etc. All shipped themes set headerText to white, so this
    // is safe.
    set("--text-on-dark", colors.headerText);
    set("--text-on-dark-muted", mixHex(colors.headerText, colors.tabBarBackground, 0.35));
    // Accent — primary brand colour, secondary as hover.
    set("--accent", colors.primaryColor);
    set("--accent-hover", colors.secondaryColor);
    set("--accent-muted", rgba(colors.primaryColor, 0.5));
    // Header bar — distinct from --bg-secondary because most themes have a
    // distinct (often coloured) header.
    set("--header-bg", colors.headerBackground);
    set("--header-text", colors.headerText);
    // Section / category emphasis (used for section title text).
    set("--category-text", colors.categoryText);
}
// "#RRGGBB" → "rgba(R,G,B,A)".
function rgba(hex, alpha) {
    const c = parseHex(hex);
    if (!c)
        return hex;
    return `rgba(${c.r}, ${c.g}, ${c.b}, ${alpha})`;
}
// Linear blend in sRGB. Used to fade contentText toward the surface for
// secondary/muted text variants — produces a "70% / 45% opacity" feel
// that works on both dark and light backgrounds without alpha overlays
// (which can interact poorly with non-solid backgrounds).
function mixHex(a, b, t) {
    const ca = parseHex(a);
    const cb = parseHex(b);
    if (!ca || !cb)
        return a;
    const r = Math.round(ca.r * (1 - t) + cb.r * t);
    const g = Math.round(ca.g * (1 - t) + cb.g * t);
    const bl = Math.round(ca.b * (1 - t) + cb.b * t);
    return `#${pad2(r)}${pad2(g)}${pad2(bl)}`;
}
// Shift each channel by `delta` (clamped). Used for the input/button
// "raised surface" variants — same primitive as the WPF Shift helper.
function shiftHex(hex, delta) {
    const c = parseHex(hex);
    if (!c)
        return hex;
    const r = Math.max(0, Math.min(255, c.r + delta));
    const g = Math.max(0, Math.min(255, c.g + delta));
    const b = Math.max(0, Math.min(255, c.b + delta));
    return `#${pad2(r)}${pad2(g)}${pad2(b)}`;
}
function parseHex(hex) {
    if (!hex || hex[0] !== "#")
        return null;
    const c = hex.replace("#", "");
    if (c.length !== 6)
        return null;
    const r = parseInt(c.substring(0, 2), 16);
    const g = parseInt(c.substring(2, 4), 16);
    const b = parseInt(c.substring(4, 6), 16);
    if (Number.isNaN(r) || Number.isNaN(g) || Number.isNaN(b))
        return null;
    return { r, g, b };
}
function pad2(n) {
    return n.toString(16).padStart(2, "0");
}
