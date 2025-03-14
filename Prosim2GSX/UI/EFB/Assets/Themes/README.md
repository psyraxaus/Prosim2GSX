# EFB Theme System

The Electronic Flight Bag (EFB) UI in Prosim2GSX supports a comprehensive theming system that allows users to customize the appearance of the interface. This document explains how the theme system works and how to create custom themes.

> **New Features (March 2025):**
> - **Enhanced Contrast Checking**: The theme system now automatically ensures proper contrast between text and background colors.
> - **Text-Specific Color Properties**: New color properties for better text visibility.
> - **High Contrast Themes**: New high contrast themes for improved accessibility.

## Theme Selection

The EFB UI determines which theme to use based on the following logic:

1. If a theme preference has been saved from a previous session, it will use that theme.
2. If no preference is saved or the saved theme is not found, it will look for a theme with the `isDefault` property set to `true`.
3. If no default theme is found, it will use the first theme it finds.
4. If no themes are found, it will use a built-in default theme.

## Theme Persistence

When you select a theme (either by cycling through themes or using a theme selector), your preference is automatically saved to the application's configuration file. The next time you start the application, it will load your preferred theme.

## Theme Files

Themes are defined in JSON files stored in the `UI/EFB/Assets/Themes` directory. Each theme file should follow the structure shown in the example themes provided.

### Theme File Structure

```json
{
  "name": "ThemeName",
  "description": "Theme description",
  "author": "Author name",
  "version": "1.0.0",
  "airlineCode": "IATA",
  "isDefault": false,
  "isDarkTheme": true,
  "creationDate": "2025-03-13T12:00:00Z",
  "lastModifiedDate": "2025-03-13T12:00:00Z",
  "colors": {
    "PrimaryColor": "#RRGGBB",
    "SecondaryColor": "#RRGGBB",
    "AccentColor": "#RRGGBB",
    "BackgroundColor": "#RRGGBB",
    "ForegroundColor": "#RRGGBB",
    ...
  },
  "fonts": {
    "PrimaryFontFamily": "Font name",
    "SecondaryFontFamily": "Font name",
    "HeaderFontFamily": "Font name",
    "MonospaceFontFamily": "Font name"
  },
  "resources": {
    "CornerRadius": "4",
    "ButtonCornerRadius": "4",
    ...
  }
}
```

### Required Properties

- `name`: The name of the theme (must be unique)
- `description`: A brief description of the theme
- `colors`: A dictionary of color values (see Required Colors below)

### Required Colors

The following colors are required for a theme to be valid:

- `PrimaryColor`: The primary color of the theme (maps to `EFBPrimaryColor` in the UI)
- `SecondaryColor`: The secondary color of the theme (maps to `EFBSecondaryColor` in the UI)
- `AccentColor`: The accent color of the theme (maps to `EFBAccentColor` in the UI)
- `BackgroundColor`: The background color of the UI (maps to `EFBBackgroundColor` in the UI)
- `ForegroundColor`: The foreground color of the UI (text color) (maps to `EFBForegroundColor` in the UI)

Additional colors you can define:

- `BorderColor`: The color of borders (maps to `EFBBorderColor` in the UI)
- `SuccessColor`: The color for success states (maps to `EFBSuccessColor` in the UI)
- `WarningColor`: The color for warning states (maps to `EFBWarningColor` in the UI)
- `ErrorColor`: The color for error states (maps to `EFBErrorColor` in the UI)
- `InfoColor`: The color for informational states (maps to `EFBInfoColor` in the UI)

> **Note:** The theme system automatically maps these color keys to the corresponding EFB resource keys used in the UI. It also automatically creates brush resources for each color (e.g., `PrimaryColor` is mapped to both `EFBPrimaryColor` and `EFBPrimaryBrush`).

### Optional Properties

- `author`: The name of the theme author
- `version`: The version of the theme
- `airlineCode`: The IATA code of the airline (for airline-specific themes)
- `isDefault`: Whether this theme should be used as the default theme (only one theme should have this set to `true`)
- `isDarkTheme`: Whether this is a dark theme (affects system UI integration)
- `creationDate`: The date the theme was created
- `lastModifiedDate`: The date the theme was last modified
- `fonts`: A dictionary of font family names
- `resources`: A dictionary of other resources (corner radii, padding, etc.)

## Creating Custom Themes

To create a custom theme:

1. Create a new JSON file in the `UI/EFB/Assets/Themes` directory with a `.json` extension.
2. Copy the structure from one of the example themes.
3. Modify the properties to match your desired theme.
4. Save the file.

The theme will be automatically loaded the next time the application starts.

## Text-Specific Color Properties

The theme system now supports additional color properties specifically for text elements to ensure proper contrast and readability:

- `EFBTextPrimaryColor`: Primary text color for most text elements
- `EFBTextSecondaryColor`: Secondary text color for less important text
- `EFBTextAccentColor`: Accent text color for highlighted text
- `EFBTextContrastColor`: Contrast text color for text on accent backgrounds
- `EFBStatusSuccessTextColor`: Text color for success messages
- `EFBStatusWarningTextColor`: Text color for warning messages
- `EFBStatusErrorTextColor`: Text color for error messages
- `EFBStatusInfoTextColor`: Text color for info messages
- `EFBStatusInactiveTextColor`: Text color for inactive elements

These properties are optional but recommended for ensuring proper text visibility. If not provided, the theme system will automatically derive appropriate values based on the background colors.

## Automatic Contrast Checking

The theme system now includes automatic contrast checking to ensure that text is always readable:

1. When a theme is loaded, the system checks the contrast between text colors and their background colors.
2. If the contrast is insufficient (below WCAG 2.0 recommendations), the system automatically adjusts the text color.
3. This ensures that text is always readable, even if the theme doesn't explicitly define good contrast colors.

For more details on contrast requirements and recommendations, see the `ThemingGuide.md` file.

## Example Themes

The following example themes are provided:

- `Default.json`: A dark theme with blue accents (marked as default)
- `Light.json`: A light theme with blue accents
- `HighContrastLight.json`: A high-contrast light theme for improved accessibility
- `HighContrastDark.json`: A high-contrast dark theme for improved accessibility

You can use these themes as a starting point for creating your own custom themes.

## Theme Switching

You can switch between themes at runtime using the theme selector or by cycling through available themes. Your theme preference will be automatically saved for the next session.
