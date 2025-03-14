# Creating Custom Airline Themes for Prosim2GSX EFB

This guide will walk you through the process of creating a custom airline theme for the Prosim2GSX Electronic Flight Bag (EFB).

## Simplified Theme Format (Recommended)

The simplified theme format requires only 5 core colors, from which all other colors are automatically derived. This makes theme creation much easier while still allowing for customization.

```json
{
  "name": "Your Airline Name",
  "description": "Description of your theme",
  "author": "Your Name",
  "version": "1.0.0",
  "airlineCode": "ABC",
  "isDefault": false,
  "isDarkTheme": true,
  "creationDate": "2025-04-01T00:00:00Z",
  "lastModifiedDate": "2025-04-01T00:00:00Z",
  "colors": {
    "PrimaryColor": "#123456",    // Main brand color
    "SecondaryColor": "#234567",  // Secondary/complementary color
    "AccentColor": "#345678",     // Highlight/call-to-action color
    "BackgroundColor": "#123456", // Main background
    "TextColor": "#FFFFFF"        // Primary text color
  },
  "fonts": {
    "PrimaryFontFamily": "Arial, sans-serif",
    "SecondaryFontFamily": "Arial, sans-serif",
    "HeaderFontFamily": "Arial, sans-serif",
    "HeaderFontWeight": "SemiBold",
    "MonospaceFontFamily": "Courier New, monospace"
  },
  "resources": {
    "CornerRadius": "4",
    "ButtonCornerRadius": "4",
    "InputCornerRadius": "4",
    "PanelCornerRadius": "4",
    "WindowCornerRadius": "4",
    "DefaultPadding": "8",
    "DefaultMargin": "8",
    "DefaultSpacing": "8",
    "DefaultBorderThickness": "1",
    "DefaultFontSize": "12",
    "HeaderFontSize": "16",
    "SubheaderFontSize": "14",
    "SmallFontSize": "11",
    "LargeFontSize": "20"
  }
}
```

### Core Colors

The simplified theme format requires only 5 core colors:

| Color | Description | Example |
|-------|-------------|---------|
| PrimaryColor | Main brand color | #003366 (Delta blue) |
| SecondaryColor | Secondary/complementary color | #001A3E (Darker blue) |
| AccentColor | Highlight/call-to-action color | #E01A33 (Delta red) |
| BackgroundColor | Main background color | #001A3E (Dark blue) |
| TextColor | Primary text color | #FFFFFF (White) |

### Automatic Color Derivation

From these 5 core colors, the system automatically derives all other colors needed for the UI:

- **Header colors**: Derived from SecondaryColor and TextColor
- **Button colors**: Normal state uses SecondaryColor, hover state is a lightened version, pressed state uses AccentColor
- **Input field colors**: Derived from BackgroundColor and TextColor
- **Border colors**: Derived from SecondaryColor
- **Text colors**: Primary, secondary, and contrast text colors are derived from TextColor and AccentColor
- **Status colors**: Default values are provided but can be overridden

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

## Legacy Theme Format (Complete)

For advanced customization, you can still use the complete theme format with all color properties specified explicitly:

```json
{
  "name": "Your Airline Name",
  "description": "Description of your theme",
  "author": "Your Name",
  "version": "1.0.0",
  "airlineCode": "ABC",
  "isDefault": false,
  "isDarkTheme": true,
  "creationDate": "2025-04-01T00:00:00Z",
  "lastModifiedDate": "2025-04-01T00:00:00Z",
  "colors": {
    "PrimaryColor": "#123456",
    "SecondaryColor": "#234567",
    "AccentColor": "#345678",
    "BackgroundColor": "#123456",
    "ForegroundColor": "#FFFFFF",
    "BorderColor": "#234567",
    "SuccessColor": "#33CC33",
    "WarningColor": "#FFAD00",
    "ErrorColor": "#FF3333",
    "InfoColor": "#3366FF",
    "HeaderBackgroundColor": "#234567",
    "HeaderForegroundColor": "#FFFFFF",
    "ButtonBackgroundColor": "#234567",
    "ButtonForegroundColor": "#FFFFFF",
    "ButtonHoverBackgroundColor": "#345678",
    "ButtonPressedBackgroundColor": "#345678",
    "ButtonPressedForegroundColor": "#FFFFFF",
    "InputBackgroundColor": "#345678",
    "InputForegroundColor": "#FFFFFF",
    "InputBorderColor": "#234567",
    "InputFocusBorderColor": "#345678",
    "EFBTextPrimaryColor": "#FFFFFF",
    "EFBTextSecondaryColor": "#CCCCCC",
    "EFBTextAccentColor": "#345678",
    "EFBTextContrastColor": "#000000",
    "EFBStatusSuccessTextColor": "#33CC33",
    "EFBStatusWarningTextColor": "#FFAD00",
    "EFBStatusErrorTextColor": "#FF3333",
    "EFBStatusInfoTextColor": "#3366FF",
    "EFBStatusInactiveTextColor": "#AAAAAA",
    "TabSelectedColor": "#345678"
  },
  "fonts": {
    "PrimaryFontFamily": "Arial, sans-serif",
    "SecondaryFontFamily": "Arial, sans-serif",
    "HeaderFontFamily": "Arial, sans-serif",
    "HeaderFontWeight": "SemiBold",
    "MonospaceFontFamily": "Courier New, monospace"
  },
  "resources": {
    "CornerRadius": "4",
    "ButtonCornerRadius": "4",
    "InputCornerRadius": "4",
    "PanelCornerRadius": "4",
    "WindowCornerRadius": "4",
    "DefaultPadding": "8",
    "DefaultMargin": "8",
    "DefaultSpacing": "8",
    "DefaultBorderThickness": "1",
    "DefaultFontSize": "12",
    "HeaderFontSize": "16",
    "SubheaderFontSize": "14",
    "SmallFontSize": "11",
    "LargeFontSize": "20"
  }
}
```

## Step-by-Step Guide

1. **Research the airline's branding**
   - Find the official colors used by the airline
   - Note the fonts used in their materials
   - Observe their UI design patterns (rounded corners, etc.)

2. **Create a new JSON file**
   - Copy the simplified template above
   - Save it as `[AirlineName].json` in the `Assets/Themes` directory

3. **Fill in the basic information**
   - Set the name, description, author, etc.
   - Set the airline code (IATA code)
   - Decide if it's a dark or light theme

4. **Define the core colors**
   - Use hex color codes (#RRGGBB)
   - Primary color should be the main airline color
   - Secondary color should be a complementary color
   - Accent color should be used for highlights and important elements
   - Background color sets the main app background
   - Text color defines the main text color

5. **Choose appropriate fonts**
   - Use fonts that are available on most systems
   - Consider the readability of the font at different sizes
   - Match the airline's typography if possible

6. **Set the resources**
   - Adjust sizes and dimensions to match the airline's style
   - Consider accessibility when setting font sizes
   - Test different corner radius values for buttons and panels

7. **Test your theme**
   - Load the theme in the EFB
   - Check all UI elements for proper styling
   - Add optional color overrides if needed

## Color Properties

### Core Colors (Simplified Format)

| Property | Description | Example |
|----------|-------------|---------|
| PrimaryColor | Main color of the airline | #003366 (Delta blue) |
| SecondaryColor | Secondary color, often used for contrast | #001A3E (Darker blue) |
| AccentColor | Used for highlights and important elements | #E01A33 (Delta red) |
| BackgroundColor | Background color for the application | #001A3E (Dark blue) |
| TextColor | Primary text color | #FFFFFF (White) |

### Derived Colors

These colors are automatically derived from the core colors but can be overridden if needed:

| Derived Property | Derivation Rule | Example |
|------------------|-----------------|---------|
| ForegroundColor | Same as TextColor | #FFFFFF |
| BorderColor | Same as SecondaryColor | #001A3E |
| HeaderBackgroundColor | Same as SecondaryColor | #001A3E |
| HeaderForegroundColor | Same as TextColor | #FFFFFF |
| ButtonBackgroundColor | Same as SecondaryColor | #001A3E |
| ButtonForegroundColor | Same as TextColor | #FFFFFF |
| ButtonHoverBackgroundColor | Lightened SecondaryColor (15%) | #00264D |
| ButtonPressedBackgroundColor | Same as AccentColor | #E01A33 |
| InputBackgroundColor | Darkened BackgroundColor (10%) | #001631 |
| InputForegroundColor | Same as TextColor | #FFFFFF |
| InputBorderColor | Same as SecondaryColor | #001A3E |
| InputFocusBorderColor | Same as AccentColor | #E01A33 |
| TabSelectedColor | Same as AccentColor | #E01A33 |

### Status Colors (Default Values)

| Property | Default Value | Example |
|----------|---------------|---------|
| SuccessColor | #33CC33 | Green |
| WarningColor | #FFCC00 | Amber |
| ErrorColor | #FF3333 | Red |
| InfoColor | #3366FF | Blue |

### Text-Specific Colors

| Property | Derivation Rule | Example |
|----------|-----------------|---------|
| EFBTextPrimaryColor | Same as TextColor | #FFFFFF |
| EFBTextSecondaryColor | TextColor with 70% opacity | #FFFFFFB3 |
| EFBTextAccentColor | Same as AccentColor | #E01A33 |
| EFBTextContrastColor | Auto-calculated for contrast | #000000 |

## Font Properties

| Property | Description | Example |
|----------|-------------|---------|
| PrimaryFontFamily | Main font for the application | Arial, sans-serif |
| SecondaryFontFamily | Secondary font, often used for specific elements | Arial, sans-serif |
| HeaderFontFamily | Font used for headers | Arial, sans-serif |
| HeaderFontWeight | Weight for header font | SemiBold |
| MonospaceFontFamily | Monospace font for code or fixed-width text | Courier New, monospace |

### Font Fallbacks

To ensure compatibility across different systems, always include fallback fonts:

```json
"fonts": {
  "PrimaryFontFamily": "Arial, sans-serif",
  "SecondaryFontFamily": "Arial, sans-serif",
  "HeaderFontFamily": "Arial, sans-serif",
  "HeaderFontWeight": "SemiBold",
  "MonospaceFontFamily": "Courier New, monospace"
}
```

This approach ensures that if the primary font is not available on the user's system, the fallback fonts will be used instead. The application automatically adds appropriate fallbacks if you specify only a single font.

## Resource Properties

| Property | Description | Example |
|----------|-------------|---------|
| CornerRadius | Radius for rounded corners | 4 |
| ButtonCornerRadius | Radius for button corners | 4 |
| InputCornerRadius | Radius for input field corners | 4 |
| PanelCornerRadius | Radius for panel corners | 4 |
| WindowCornerRadius | Radius for window corners | 4 |
| DefaultPadding | Default padding for elements | 8 |
| DefaultMargin | Default margin for elements | 8 |
| DefaultSpacing | Default spacing between elements | 8 |
| DefaultBorderThickness | Default thickness for borders | 1 |
| DefaultFontSize | Default font size | 12 |
| HeaderFontSize | Size for header text | 16 |
| SubheaderFontSize | Size for subheader text | 14 |
| SmallFontSize | Size for small text | 11 |
| LargeFontSize | Size for large text | 20 |

## Tips for Color Selection

- Use the airline's official colors whenever possible
- Ensure sufficient contrast between text and background colors
- Consider color blindness and accessibility
- Test your theme in different lighting conditions
- For dark themes, use darker colors for BackgroundColor and SecondaryColor
- For light themes, use lighter colors for BackgroundColor and darker colors for TextColor

## Automatic Contrast Handling

The simplified theme system includes automatic contrast checking and adjustment to ensure readability:

1. **Text Color Contrast**: The system automatically checks the contrast between text colors and their backgrounds
2. **Automatic Adjustments**: If contrast is insufficient, colors are automatically adjusted
3. **Smart Derivation**: Text colors on colored backgrounds are derived to ensure readability

### WCAG 2.0 Contrast Guidelines

The Web Content Accessibility Guidelines (WCAG) 2.0 recommend the following minimum contrast ratios:

- **4.5:1** for normal text (12pt or 16px and below)
- **3:1** for large text (14pt/18.5px bold or 18pt/24px normal and above)
- **3:1** for UI components and graphical objects

The theme system automatically enforces these guidelines.

### How Contrast is Ensured

1. **Background Analysis**: The system analyzes the luminance of background colors
2. **Smart Text Colors**: For dark backgrounds, lighter text is used; for light backgrounds, darker text is used
3. **Contrast Calculation**: The system calculates the contrast ratio between text and background
4. **Automatic Adjustment**: If contrast is insufficient, colors are adjusted until they meet guidelines
5. **Fallback to Black/White**: If adjustments can't achieve sufficient contrast, the system falls back to black or white

This automatic handling means you don't need to worry about contrast issues when creating themes with the simplified format.

## High Contrast Themes

The application includes high contrast themes for users who need better visibility:

- **High Contrast Light**: A light theme with enhanced contrast
- **High Contrast Dark**: A dark theme with enhanced contrast

These themes use:

- Larger font sizes
- Bolder text
- Higher contrast color combinations
- Thicker borders

You can use these themes as references when designing your own accessible themes.

## Troubleshooting

- **Theme doesn't load**: Check your JSON syntax for errors
- **Colors look wrong**: Verify hex codes are in the correct format (#RRGGBB)
- **Text is hard to read**: Try adjusting your core colors for better contrast
- **UI elements look strange**: Check your resource values for appropriate sizes
- **Font rendering issues**: 
  - Always use font families with fallbacks (e.g., "Arial, sans-serif")
  - Avoid using fonts that might not be installed on all systems
  - If you see "FontFamily not valid" errors, check that you're using generic fallbacks
  - For icon fonts, ensure you have appropriate fallbacks specified

## Example: Creating a Lufthansa Theme

1. **Research Lufthansa branding**
   - Primary color: #05164D (dark blue)
   - Secondary color: #FFAD00 (yellow/gold)
   - Typography: Clean, sans-serif fonts

2. **Create the JSON file**
   - Save as `Lufthansa.json` in the `Assets/Themes` directory

3. **Define the colors**
   ```json
   "colors": {
     "PrimaryColor": "#05164D",
     "SecondaryColor": "#041033",
     "AccentColor": "#FFAD00",
     "BackgroundColor": "#05164D",
     "ForegroundColor": "#FFFFFF",
     "BorderColor": "#041033",
     "SuccessColor": "#33CC33",
     "WarningColor": "#FFAD00",
     "ErrorColor": "#FF3333",
     "InfoColor": "#3366FF",
     "HeaderBackgroundColor": "#041033",
     "HeaderForegroundColor": "#FFFFFF",
     "ButtonBackgroundColor": "#041033",
     "ButtonForegroundColor": "#FFFFFF",
     "ButtonHoverBackgroundColor": "#072169",
     "ButtonPressedBackgroundColor": "#FFAD00",
     "ButtonPressedForegroundColor": "#000000",
     "InputBackgroundColor": "#072169",
     "InputForegroundColor": "#FFFFFF",
     "InputBorderColor": "#041033",
     "InputFocusBorderColor": "#FFAD00",
     "EFBTextPrimaryColor": "#FFFFFF",
     "EFBTextSecondaryColor": "#CCCCCC",
     "EFBTextAccentColor": "#FFAD00",
     "EFBTextContrastColor": "#000000",
     "EFBStatusSuccessTextColor": "#33CC33",
     "EFBStatusWarningTextColor": "#FFAD00",
     "EFBStatusErrorTextColor": "#FF3333",
     "EFBStatusInfoTextColor": "#3366FF",
     "EFBStatusInactiveTextColor": "#AAAAAA",
     "TabSelectedColor": "#FFAD00"
   }
   ```

4. **Choose fonts**
   ```json
   "fonts": {
     "PrimaryFontFamily": "Arial, sans-serif",
     "SecondaryFontFamily": "Arial, sans-serif",
     "HeaderFontFamily": "Arial, sans-serif",
     "HeaderFontWeight": "SemiBold",
     "MonospaceFontFamily": "Courier New, monospace"
   }
   ```

5. **Set resources**
   ```json
   "resources": {
     "CornerRadius": 4,
     "ButtonHeight": 32,
     "TabHeight": 40,
     "HeaderHeight": 48,
     "DefaultMargin": 8,
     "DefaultPadding": 8,
     "SmallFontSize": 11,
     "DefaultFontSize": 12,
     "LargeFontSize": 14,
     "HeaderFontSize": 16,
     "TitleFontSize": 20
   }
   ```

6. **Test the theme**
   - Load the theme in the EFB
   - Check all UI elements for proper styling
   - Make adjustments as needed

## Flight Phase Indicator Theming

The Flight Phase Indicator is a key component of the EFB UI that displays the current and predicted flight phases. It has its own set of theme resources that control its appearance:

| Property | Description | Default Value |
|----------|-------------|---------------|
| PhaseDetailsForeground | Text color for phase details | Same as EFBTextPrimaryColor |
| PhaseItemForeground | Text color for phase items | Same as EFBTextPrimaryColor |
| ActivePhaseItemForeground | Text color for active phase item | Same as EFBTextContrastBrush |
| PredictedPhaseItemForeground | Text color for predicted phase item | Same as EFBTextSecondaryColor |
| PhaseDetailsBackground | Background color for phase details | Derived from EFBSecondaryColor |
| PhaseDetailsBorderBrush | Border color for phase details | Same as EFBBorderColor |
| PhaseItemBackground | Background color for phase items | Derived from EFBSecondaryColor |
| PhaseItemBorderBrush | Border color for phase items | Same as EFBBorderColor |
| ActivePhaseItemBackground | Background color for active phase item | Same as EFBPrimaryColor |
| ActivePhaseItemBorderBrush | Border color for active phase item | Same as EFBPrimaryColor |
| PredictedPhaseItemBackground | Background color for predicted phase item | Derived from EFBSecondaryColor |
| PredictedPhaseItemBorderBrush | Border color for predicted phase item | Same as EFBBorderColor |
| PhaseConnectorStroke | Stroke color for phase connectors | Same as EFBBorderColor |
| ActivePhaseConnectorStroke | Stroke color for active phase connector | Same as EFBPrimaryColor |
| PredictedPhaseConnectorStroke | Stroke color for predicted phase connector | Same as EFBBorderColor |

### Flight Phase Indicator in Simplified Themes

When using the simplified theme format, these colors are automatically derived from your core colors. However, you can override any of them by explicitly including them in your theme:

```json
"colors": {
  "PrimaryColor": "#123456",
  "SecondaryColor": "#234567",
  "AccentColor": "#345678",
  "BackgroundColor": "#123456",
  "TextColor": "#FFFFFF",
  
  // Optional Flight Phase Indicator overrides
  "PhaseDetailsForeground": "#FFFFFF",
  "ActivePhaseItemBackground": "#FF9900",
  "ActivePhaseConnectorStroke": "#FF9900"
}
```

### Flight Phase Indicator Styling Tips

- **Ensure contrast**: Make sure text colors have sufficient contrast with their backgrounds
- **Highlight active phase**: Use a distinctive color for the active phase item and connector
- **Subtle prediction styling**: Use a more subtle style for predicted phases
- **Consistent colors**: Use colors that match your overall theme

## Conclusion

Creating custom airline themes for the Prosim2GSX EFB is a straightforward process that allows you to personalize your experience. By following this guide, you can create themes that match your favorite airlines or create entirely new designs.

If you encounter any issues or have questions, please refer to the troubleshooting section or contact support.
