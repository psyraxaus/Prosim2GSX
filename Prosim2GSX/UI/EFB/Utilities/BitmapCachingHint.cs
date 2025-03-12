using System;

namespace Prosim2GSX.UI.EFB.Utilities
{
    /// <summary>
    /// Specifies how bitmap caching should be applied to a UIElement.
    /// </summary>
    public enum BitmapCachingHint
    {
        /// <summary>
        /// Use default caching behavior.
        /// </summary>
        Default,
        
        /// <summary>
        /// Optimize for speed.
        /// </summary>
        Speed,
        
        /// <summary>
        /// Optimize for quality.
        /// </summary>
        Quality,
        
        /// <summary>
        /// Optimize for memory usage.
        /// </summary>
        Memory
    }
}
