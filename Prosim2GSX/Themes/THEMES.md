# Prosim2GSX Custom Themes

Prosim2GSX supports fully customisable themes via JSON files. Themes are loaded at startup from the `Themes` folder located next to the application executable. No recompilation or file editing is required — simply drop a new `.json` file in the folder, restart the application, and select your theme from **App Settings → Theme**.

---

## File location

```
<install dir>\Themes\
    Light.json        ← built-in light theme
    Dark.json         ← built-in dark theme
    MyAirline.json    ← your custom theme
```

The application ships with the following themes as examples:

| File | Theme |
|---|---|
| `Light.json` | Default light theme |
| `Dark.json` | Dark theme for low-light environments |
| `Delta.json` | Delta Air Lines inspired |
| `Finnair.json` | Finnair inspired |
| `Lufthansa.json` | Lufthansa inspired |
| `Qantas.json` | Qantas inspired |

---

## JSON structure

Every theme file must be valid JSON with the following structure:

```json
{
  "name": "My Theme",
  "description": "A short description shown in the UI",
  "colors": {
    "primaryColor":       "#1E90FF",
    "secondaryColor":     "#0078D7",
    "accentColor":        "#FF4500",
    "headerBackground":   "#1E90FF",
    "tabBarBackground":   "#0D1A2A",
    "contentBackground":  "#F8F8F8",
    "sectionBackground":  "#FFFFFF",
    "headerText":         "#FFFFFF",
    "contentText":        "#333333",
    "categoryText":       "#1E90FF",
    "flightPhaseColors": {
      "atGate":   "#1E90FF",
      "taxiOut":  "#FF9800",
      "inFlight": "#4CAF50",
      "approach": "#9C27B0",
      "arrived":  "#009688"
    }
  }
}
```

All colour values are standard CSS hex strings. Both 6-digit (`#RRGGBB`) and 8-digit (`#AARRGGBB`) formats are accepted. Alpha values less than `FF` make colours semi-transparent — generally only useful for `accentColor`.

`//` line comments are allowed in the JSON and are stripped before parsing, so you can annotate your file freely.

---

## Colour reference

### Core palette

| Key | Where it appears |
|---|---|
| `primaryColor` | Button highlights, selected tab indicator, active links, ComboBox selection highlight |
| `secondaryColor` | Secondary interactive elements (currently reserved for future use) |
| `accentColor` | Accent details (currently reserved for future use) |

### Surfaces

| Key | Where it appears |
|---|---|
| `headerBackground` | Top header bar of each settings section |
| `tabBarBackground` | Left-hand navigation tab strip |
| `contentBackground` | Page background behind all cards |
| `sectionBackground` | Individual card / section background |

> **Derived colours** — `InputBackground`, `ButtonBackground`, and `InputBorderBrush` are computed automatically from `sectionBackground` at runtime. You do not need to specify them. On dark themes (average RGB of `sectionBackground` < 128) inputs are lightened; on light themes they are darkened slightly so they are always distinguishable from the card behind them.

### Text

| Key | Where it appears |
|---|---|
| `headerText` | Text on `headerBackground` (section headers, tab labels) |
| `contentText` | All body text, labels, TextBox / ComboBox content, DataGrid rows |
| `categoryText` | Section category headings within a card |

### Flight phase colours

These colours are used on the **Flight Status** monitor page to colour-code the current flight phase indicator.

| Key | Phase |
|---|---|
| `atGate` | Parked at gate / pre-departure |
| `taxiOut` | Taxiing out |
| `inFlight` | Airborne |
| `approach` | On approach |
| `arrived` | Arrived / post-landing |

### Fixed colours (not themeable)

The following status indicator colours are the same in every theme and cannot be overridden via JSON:

| Colour | Meaning |
|---|---|
| Green | Active / running |
| Gold | Completed |
| Dodger Blue (`#1E90FF`) | Waiting |
| Red | Disconnected / error |
| Light Gray | Inactive / disabled |

---

## Dark theme guidelines

When `sectionBackground` has an average channel value below 128 the theme is considered **dark** and the following adjustments are applied automatically:

- `InputBackground` = `sectionBackground` brightened by ~22
- `ButtonBackground` = `sectionBackground` brightened by ~12
- `InputBorderBrush` = `#555555`

For a good dark theme:

- Keep `contentBackground` noticeably darker than `sectionBackground` so the page depth is visible (e.g. `#1E1E1E` vs `#2D2D2D`).
- Use a light `contentText` (e.g. `#E0E0E0`) — dark text on a dark card is the most common mistake.
- Use a lighter shade of your `primaryColor` for `categoryText` so it remains readable (e.g. `#5AABFF` instead of `#1E90FF`).
- Keep `headerBackground` slightly lighter than `tabBarBackground` to give the header visual separation.

---

## Example: airline-branded light theme

```json
{
  "name": "British Airways",
  "description": "British Airways inspired theme",
  "colors": {
    "primaryColor":      "#003B6F",  // BA navy
    "secondaryColor":    "#003B6F",
    "accentColor":       "#D40E14",  // BA red
    "headerBackground":  "#003B6F",
    "tabBarBackground":  "#001F3D",
    "contentBackground": "#F5F5F5",
    "sectionBackground": "#FFFFFF",
    "headerText":        "#FFFFFF",
    "contentText":       "#1A1A1A",
    "categoryText":      "#003B6F",
    "flightPhaseColors": {
      "atGate":   "#003B6F",
      "taxiOut":  "#D40E14",
      "inFlight": "#4CAF50",
      "approach": "#9C27B0",
      "arrived":  "#009688"
    }
  }
}
```

## Example: dark theme

```json
{
  "name": "Midnight",
  "description": "High-contrast dark theme",
  "colors": {
    "primaryColor":      "#00BFFF",
    "secondaryColor":    "#007ACC",
    "accentColor":       "#FF6A33",
    "headerBackground":  "#1A2A3A",
    "tabBarBackground":  "#080808",
    "contentBackground": "#121212",
    "sectionBackground": "#1E1E1E",
    "headerText":        "#FFFFFF",
    "contentText":       "#EEEEEE",
    "categoryText":      "#4FC3F7",
    "flightPhaseColors": {
      "atGate":   "#4FC3F7",
      "taxiOut":  "#FFB74D",
      "inFlight": "#81C784",
      "approach": "#CE93D8",
      "arrived":  "#4DB6AC"
    }
  }
}
```

---

## Applying a theme

1. Copy your `.json` file into the `Themes` folder next to `Prosim2GSX.exe`.
2. Start (or restart) Prosim2GSX.
3. Open **App Settings** and select your theme from the **Theme** dropdown.
4. The theme is applied immediately and saved for the next session.

To edit a theme while the app is running you can use the **Refresh Themes** button in App Settings — changes to the JSON are picked up without restarting.
