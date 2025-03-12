# Creating Custom Airline Themes for Prosim2GSX EFB

This guide will walk you through the process of creating a custom airline theme for the Prosim2GSX Electronic Flight Bag (EFB).

## Theme JSON Structure

Themes are defined in JSON files with the following structure:

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
    "HeaderColor": "#234567",
    "TabColor": "#234567",
    "TabSelectedColor": "#345678",
    "ButtonColor": "#234567",
    "ButtonHoverColor": "#345678",
    "ButtonPressedColor": "#345678",
    "TextBoxColor": "#345678",
    "TextBoxBorderColor": "#234567",
    "TextBoxFocusedColor": "#345678",
    "ErrorColor": "#FF3333",
    "WarningColor": "#FFAD00",
    "SuccessColor": "#33CC33",
    "InfoColor": "#3366FF"
  },
  "fonts": {
    "PrimaryFont": "Segoe UI",
    "SecondaryFont": "Consolas",
    "HeaderFont": "Segoe UI Semibold",
    "MonospaceFont": "Consolas"
  },
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
}
```

## Step-by-Step Guide

1. **Research the airline's branding**
   - Find the official colors used by the airline
   - Note the fonts used in their materials
   - Observe their UI design patterns (rounded corners, etc.)

2. **Create a new JSON file**
   - Copy the template above
   - Save it as `[AirlineName].json` in the `Assets/Themes` directory

3. **Fill in the basic information**
   - Set the name, description, author, etc.
   - Set the airline code (IATA code)
   - Decide if it's a dark or light theme

4. **Define the colors**
   - Use hex color codes (#RRGGBB)
   - Primary color should be the main airline color
   - Secondary color should be a complementary color
   - Accent color should be used for highlights and important elements

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
   - Make adjustments as needed

## Color Properties

| Property | Description | Example |
|----------|-------------|---------|
| PrimaryColor | Main color of the airline | #003366 (Delta blue) |
| SecondaryColor | Secondary color, often used for contrast | #E01A33 (Delta red) |
| AccentColor | Used for highlights and important elements | #E01A33 |
| BackgroundColor | Background color for the application | #FFFFFF (white) |
| ForegroundColor | Text color | #333333 (dark gray) |
| BorderColor | Color for borders | #D9D9D9 (light gray) |
| HeaderColor | Color for headers | #003366 |
| TabColor | Color for inactive tabs | #F2F2F2 (very light gray) |
| TabSelectedColor | Color for selected tabs | #003366 |
| ButtonColor | Base color for buttons | #003366 |
| ButtonHoverColor | Color when hovering over buttons | #00264D (darker blue) |
| ButtonPressedColor | Color when pressing buttons | #001A33 (even darker blue) |
| TextBoxColor | Background color for text boxes | #FFFFFF |
| TextBoxBorderColor | Border color for text boxes | #D9D9D9 |
| TextBoxFocusedColor | Border color for focused text boxes | #003366 |
| ErrorColor | Color for error messages | #E01A33 (red) |
| WarningColor | Color for warning messages | #F7A800 (amber) |
| SuccessColor | Color for success messages | #00A650 (green) |
| InfoColor | Color for informational messages | #0072CE (blue) |

## Font Properties

| Property | Description | Example |
|----------|-------------|---------|
| PrimaryFont | Main font for the application | Segoe UI |
| SecondaryFont | Secondary font, often used for specific elements | Consolas |
| HeaderFont | Font used for headers | Segoe UI Semibold |
| MonospaceFont | Monospace font for code or fixed-width text | Consolas |

## Resource Properties

| Property | Description | Example |
|----------|-------------|---------|
| CornerRadius | Radius for rounded corners | 4 |
| ButtonHeight | Height of buttons | 32 |
| TabHeight | Height of tabs | 40 |
| HeaderHeight | Height of headers | 48 |
| DefaultMargin | Default margin for elements | 8 |
| DefaultPadding | Default padding for elements | 8 |
| SmallFontSize | Size for small text | 11 |
| DefaultFontSize | Default font size | 12 |
| LargeFontSize | Size for large text | 14 |
| HeaderFontSize | Size for header text | 16 |
| TitleFontSize | Size for title text | 20 |

## Tips for Color Selection

- Use the airline's official colors whenever possible
- Ensure sufficient contrast between text and background colors
- Consider color blindness and accessibility
- Test your theme in different lighting conditions

## Troubleshooting

- **Theme doesn't load**: Check your JSON syntax for errors
- **Colors look wrong**: Verify hex codes are in the correct format (#RRGGBB)
- **Text is hard to read**: Adjust the contrast between text and background colors
- **UI elements look strange**: Check your resource values for appropriate sizes

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
     "HeaderColor": "#041033",
     "TabColor": "#041033",
     "TabSelectedColor": "#FFAD00",
     "ButtonColor": "#041033",
     "ButtonHoverColor": "#072169",
     "ButtonPressedColor": "#FFAD00",
     "TextBoxColor": "#072169",
     "TextBoxBorderColor": "#041033",
     "TextBoxFocusedColor": "#FFAD00",
     "ErrorColor": "#FF3333",
     "WarningColor": "#FFAD00",
     "SuccessColor": "#33CC33",
     "InfoColor": "#3366FF"
   }
   ```

4. **Choose fonts**
   ```json
   "fonts": {
     "PrimaryFont": "Segoe UI",
     "SecondaryFont": "Consolas",
     "HeaderFont": "Segoe UI Semibold",
     "MonospaceFont": "Consolas"
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

## Conclusion

Creating custom airline themes for the Prosim2GSX EFB is a straightforward process that allows you to personalize your experience. By following this guide, you can create themes that match your favorite airlines or create entirely new designs.

If you encounter any issues or have questions, please refer to the troubleshooting section or contact support.
