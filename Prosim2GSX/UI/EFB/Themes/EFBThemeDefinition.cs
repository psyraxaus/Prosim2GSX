using System;
using System.Collections.Generic;
using System.Windows;

namespace Prosim2GSX.UI.EFB.Themes
{
    /// <summary>
    /// Defines a theme for the EFB UI.
    /// </summary>
    public class EFBThemeDefinition
    {
        private readonly Dictionary<string, object> _resources = new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the <see cref="EFBThemeDefinition"/> class.
        /// </summary>
        /// <param name="name">The theme name.</param>
        public EFBThemeDefinition(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Gets the theme name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the theme description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the path to the theme logo.
        /// </summary>
        public string LogoPath { get; set; }

        /// <summary>
        /// Gets or sets the path to the theme resource dictionary.
        /// </summary>
        public string ResourceDictionaryPath { get; set; }

        /// <summary>
        /// Gets or sets the theme resource dictionary.
        /// </summary>
        public ResourceDictionary ResourceDictionary { get; set; }

        /// <summary>
        /// Gets or sets a resource value.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <returns>The resource value.</returns>
        public object this[string key]
        {
            get => _resources.TryGetValue(key, out var value) ? value : null;
            set => _resources[key] = value;
        }

        /// <summary>
        /// Gets a resource value.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <returns>The resource value.</returns>
        public object GetResource(string key)
        {
            return this[key];
        }

        /// <summary>
        /// Sets a resource value.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="value">The resource value.</param>
        public void SetResource(string key, object value)
        {
            this[key] = value;
        }

        /// <summary>
        /// Determines whether the theme contains a resource with the specified key.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <returns>True if the theme contains a resource with the specified key, false otherwise.</returns>
        public bool ContainsResource(string key)
        {
            return _resources.ContainsKey(key);
        }

        /// <summary>
        /// Removes a resource with the specified key.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <returns>True if the resource was removed, false otherwise.</returns>
        public bool RemoveResource(string key)
        {
            return _resources.Remove(key);
        }

        /// <summary>
        /// Clears all resources.
        /// </summary>
        public void ClearResources()
        {
            _resources.Clear();
        }

        /// <summary>
        /// Gets all resource keys.
        /// </summary>
        /// <returns>A collection of all resource keys.</returns>
        public IEnumerable<string> GetResourceKeys()
        {
            return _resources.Keys;
        }

        /// <summary>
        /// Gets all resource values.
        /// </summary>
        /// <returns>A collection of all resource values.</returns>
        public IEnumerable<object> GetResourceValues()
        {
            return _resources.Values;
        }

        /// <summary>
        /// Gets all resources.
        /// </summary>
        /// <returns>A collection of all resources.</returns>
        public IEnumerable<KeyValuePair<string, object>> GetResources()
        {
            return _resources;
        }
    }
}
