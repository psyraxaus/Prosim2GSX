# Prosim2GSX EFB Themes

This directory contains themes for the Prosim2GSX Electronic Flight Bag (EFB). Themes control the visual appearance of the EFB, including colors, fonts, and other visual elements.

## Simplified Theme System

The EFB now supports a simplified theme format that requires only 5 core colors, from which all other colors are automatically derived. This makes theme creation much easier while still allowing for customization.

### Core Colors

The simplified theme format requires only these 5 core colors:

1. **PrimaryColor**: Main brand color
2. **SecondaryColor**: Secondary/complementary color
3. **AccentColor**: Highlight/call-to-action color
4. **BackgroundColor**: Main background color
5. **TextColor**: Primary text color

### Example

```json
{
  "name": "Airline Name",
  "description": "Description of your theme",
  "author": "Your Name",
  "version": "1.0.0",
  "airlineCode": "ABC",
  "isDarkTheme": true,
  "colors": {
    "PrimaryColor": "#123456",
    "SecondaryColor": "#234567",
    "AccentColor": "#345678",
    "BackgroundColor": "#123456",
    "TextColor": "#FFFFFF"
  }
}
```

### Automatic Derivation

From these 5 core colors, the system automatically derives all other colors needed for the UI:

- **Header colors**: Derived from SecondaryColor and TextColor
- **Button colors**: Normal state uses SecondaryColor, hover state is a lightened version, pressed state uses AccentColor
- **Input field colors**: Derived from BackgroundColor and TextColor
- **Border colors**: Derived from SecondaryColor
- **Text colors**: Primary, secondary, and contrast text colors are derived from TextColor and AccentColor

### Optional Overrides

If you need more control over specific colors, you can override any automatically derived color by including it explicitly in your theme:

```json
"colors": {
  "PrimaryColor": "#123456",
  "SecondaryColor": "#234567",
  "AccentColor": "#345678",
  "BackgroundColor": "#123456",
  "TextColor": "#FFFFFF",
  
  // Optional overrides
  "ButtonHoverBackgroundColor": "#003366",
  "ButtonPressedBackgroundColor": "#CE1A39"
}
```

## Available Themes

### Default Themes

- **Default**: The default theme for the EFB
- **Light**: A light theme with blue accents
- **HighContrastDark**: A high contrast dark theme for better visibility
- **HighContrastLight**: A high contrast light theme for better visibility

### Airline Themes

- **AirFrance**: Air France-themed EFB
- **BritishAirways**: British Airways-themed EFB
- **CathayPacific**: Cathay Pacific-themed EFB
- **DeltaAirLines**: Delta Air Lines-themed EFB
- **Emirates**: Emirates-themed EFB
- **Finnair**: Finnair-themed EFB
- **KLM**: KLM Royal Dutch Airlines-themed EFB
- **Lufthansa**: Lufthansa-themed EFB
- **Qantas**: Qantas-themed EFB
- **SingaporeAirlines**: Singapore Airlines-themed EFB

## Creating Your Own Theme

1. Copy one of the simplified theme examples (e.g., `BritishAirways.json`)
2. Rename it to your airline or preferred name (e.g., `MyAirline.json`)
3. Edit the core colors to match your preferred colors
4. Save the file in this directory
5. The theme will be available in the EFB settings

For more detailed instructions, see the [ThemingGuide.md](ThemingGuide.md) file.

## Theme Format Reference

For a complete reference of all available theme properties, see the [ThemingGuide.md](ThemingGuide.md) file.
