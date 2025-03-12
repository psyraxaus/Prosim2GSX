using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Prosim2GSX.UI.EFB.Themes
{
    /// <summary>
    /// Defines a theme for the EFB UI.
    /// </summary>
    public class EFBThemeDefinition
    {
        /// <summary>
        /// Gets or sets the name of the theme.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the theme.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the author of the theme.
        /// </summary>
        [JsonPropertyName("author")]
        public string Author { get; set; }

        /// <summary>
        /// Gets or sets the version of the theme.
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the airline code associated with the theme.
        /// </summary>
        [JsonPropertyName("airlineCode")]
        public string AirlineCode { get; set; }

        /// <summary>
        /// Gets or sets the colors for the theme.
        /// </summary>
        [JsonPropertyName("colors")]
        public Dictionary<string, string> Colors { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the fonts for the theme.
        /// </summary>
        [JsonPropertyName("fonts")]
        public Dictionary<string, string> Fonts { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the resources for the theme.
        /// These are additional resources that can be used by the theme.
        /// </summary>
        [JsonPropertyName("resources")]
        public Dictionary<string, object> Resources { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the path to the logo for the theme.
        /// </summary>
        [JsonPropertyName("logoPath")]
        public string LogoPath { get; set; }

        /// <summary>
        /// Gets or sets the path to the background image for the theme.
        /// </summary>
        [JsonPropertyName("backgroundPath")]
        public string BackgroundPath { get; set; }

        /// <summary>
        /// Gets or sets the base theme that this theme extends.
        /// </summary>
        [JsonPropertyName("baseTheme")]
        public string BaseTheme { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this theme is a dark theme.
        /// </summary>
        [JsonPropertyName("isDarkTheme")]
        public bool IsDarkTheme { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this theme is the default theme.
        /// </summary>
        [JsonPropertyName("isDefault")]
        public bool IsDefault { get; set; }

        /// <summary>
        /// Gets or sets the creation date of the theme.
        /// </summary>
        [JsonPropertyName("creationDate")]
        public DateTime CreationDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the last modified date of the theme.
        /// </summary>
        [JsonPropertyName("lastModifiedDate")]
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the path to the theme file.
        /// </summary>
        [JsonIgnore]
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this theme has been loaded.
        /// </summary>
        [JsonIgnore]
        public bool IsLoaded { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this theme has validation errors.
        /// </summary>
        [JsonIgnore]
        public bool HasValidationErrors { get; set; }

        /// <summary>
        /// Gets or sets the validation errors for this theme.
        /// </summary>
        [JsonIgnore]
        public List<string> ValidationErrors { get; set; } = new List<string>();

        /// <summary>
        /// Validates the theme definition.
        /// </summary>
        /// <returns>True if the theme is valid, false otherwise.</returns>
        public bool Validate()
        {
            ValidationErrors.Clear();
            HasValidationErrors = false;

            // Check required properties
            if (string.IsNullOrWhiteSpace(Name))
            {
                ValidationErrors.Add("Theme name is required.");
                HasValidationErrors = true;
            }

            if (string.IsNullOrWhiteSpace(Version))
            {
                ValidationErrors.Add("Theme version is required.");
                HasValidationErrors = true;
            }

            // Check required colors
            var requiredColors = new[]
            {
                "PrimaryColor",
                "SecondaryColor",
                "AccentColor",
                "BackgroundColor",
                "ForegroundColor",
                "BorderColor"
            };

            foreach (var color in requiredColors)
            {
                if (!Colors.ContainsKey(color))
                {
                    ValidationErrors.Add($"Required color '{color}' is missing.");
                    HasValidationErrors = true;
                }
            }

            // Check required fonts
            var requiredFonts = new[]
            {
                "PrimaryFont",
                "SecondaryFont"
            };

            foreach (var font in requiredFonts)
            {
                if (!Fonts.ContainsKey(font))
                {
                    ValidationErrors.Add($"Required font '{font}' is missing.");
                    HasValidationErrors = true;
                }
            }

            return !HasValidationErrors;
        }
    }
}
