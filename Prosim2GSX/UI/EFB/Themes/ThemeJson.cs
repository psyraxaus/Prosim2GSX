using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Prosim2GSX.UI.EFB.Themes
{
    /// <summary>
    /// Represents a theme definition in JSON format.
    /// </summary>
    public class ThemeJson
    {
        /// <summary>
        /// Gets or sets the theme name.
        /// </summary>
        [Required]
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the theme description.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the theme author.
        /// </summary>
        [JsonProperty("author")]
        public string Author { get; set; }

        /// <summary>
        /// Gets or sets the theme version.
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the airline code.
        /// </summary>
        [JsonProperty("airlineCode")]
        public string AirlineCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the default theme.
        /// </summary>
        [JsonProperty("isDefault")]
        public bool IsDefault { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a dark theme.
        /// </summary>
        [JsonProperty("isDarkTheme")]
        public bool IsDarkTheme { get; set; }

        /// <summary>
        /// Gets or sets the theme creation date.
        /// </summary>
        [JsonProperty("creationDate")]
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Gets or sets the theme last modified date.
        /// </summary>
        [JsonProperty("lastModifiedDate")]
        public DateTime LastModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the theme colors.
        /// </summary>
        [Required]
        [JsonProperty("colors")]
        public Dictionary<string, string> Colors { get; set; }

        /// <summary>
        /// Gets or sets the theme fonts.
        /// </summary>
        [JsonProperty("fonts")]
        public Dictionary<string, string> Fonts { get; set; }

        /// <summary>
        /// Gets or sets the theme resources.
        /// </summary>
        [JsonProperty("resources")]
        public Dictionary<string, object> Resources { get; set; }
    }
}
