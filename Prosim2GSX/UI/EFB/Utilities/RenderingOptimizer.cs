using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Prosim2GSX.UI.EFB.Utilities
{
    /// <summary>
    /// Provides methods to optimize rendering performance in WPF applications.
    /// </summary>
    public static class RenderingOptimizer
    {
        /// <summary>
        /// Applies bitmap caching to a UIElement to improve rendering performance.
        /// </summary>
        /// <param name="element">The element to optimize.</param>
        /// <param name="cachingHint">The bitmap caching hint to use.</param>
        public static void ApplyBitmapCache(UIElement element, BitmapCachingHint cachingHint = BitmapCachingHint.Default)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            RenderOptions.SetBitmapScalingMode(element, BitmapScalingMode.HighQuality);
            RenderOptions.SetCachingHint(element, cachingHint);
            RenderOptions.SetCacheInvalidationThresholdMinimum(element, 0.5);
            RenderOptions.SetCacheInvalidationThresholdMaximum(element, 2.0);
            
            var cache = new BitmapCache
            {
                EnableClearType = true,
                SnapsToDevicePixels = true,
                RenderAtScale = 1.0
            };
            
            element.CacheMode = cache;
        }

        /// <summary>
        /// Removes bitmap caching from a UIElement.
        /// </summary>
        /// <param name="element">The element to remove caching from.</param>
        public static void RemoveBitmapCache(UIElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            element.CacheMode = null;
        }

        /// <summary>
        /// Optimizes an animation for performance.
        /// </summary>
        /// <param name="animation">The animation to optimize.</param>
        public static void OptimizeAnimation(Timeline animation)
        {
            if (animation == null)
                throw new ArgumentNullException(nameof(animation));

            // Set animation to use discrete frames for better performance
            Timeline.SetDesiredFrameRate(animation, 30);
        }

        /// <summary>
        /// Optimizes a storyboard for performance.
        /// </summary>
        /// <param name="storyboard">The storyboard to optimize.</param>
        public static void OptimizeStoryboard(Storyboard storyboard)
        {
            if (storyboard == null)
                throw new ArgumentNullException(nameof(storyboard));

            // Set storyboard to use discrete frames for better performance
            Timeline.SetDesiredFrameRate(storyboard, 30);
            
            // Optimize all child animations
            foreach (var child in storyboard.Children)
            {
                OptimizeAnimation(child);
            }
        }

        /// <summary>
        /// Enables hardware acceleration for a UIElement.
        /// </summary>
        /// <param name="element">The element to enable hardware acceleration for.</param>
        public static void EnableHardwareAcceleration(UIElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            // Enable hardware acceleration
            RenderOptions.SetEdgeMode(element, EdgeMode.Aliased);
        }

        /// <summary>
        /// Disables hardware acceleration for a UIElement.
        /// </summary>
        /// <param name="element">The element to disable hardware acceleration for.</param>
        public static void DisableHardwareAcceleration(UIElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            // Disable hardware acceleration
            RenderOptions.SetEdgeMode(element, EdgeMode.Unspecified);
        }

        /// <summary>
        /// Optimizes a ScrollViewer for performance.
        /// </summary>
        /// <param name="scrollViewer">The ScrollViewer to optimize.</param>
        public static void OptimizeScrollViewer(System.Windows.Controls.ScrollViewer scrollViewer)
        {
            if (scrollViewer == null)
                throw new ArgumentNullException(nameof(scrollViewer));

            // Apply bitmap cache to the scroll content
            if (scrollViewer.Content is UIElement content)
            {
                ApplyBitmapCache(content);
            }
            
            // Set scroll behavior for better performance
            scrollViewer.CanContentScroll = true;
            scrollViewer.IsDeferredScrollingEnabled = true;
        }

        /// <summary>
        /// Optimizes an ItemsControl for performance.
        /// </summary>
        /// <param name="itemsControl">The ItemsControl to optimize.</param>
        public static void OptimizeItemsControl(System.Windows.Controls.ItemsControl itemsControl)
        {
            if (itemsControl == null)
                throw new ArgumentNullException(nameof(itemsControl));

            // Use virtualization for better performance
            System.Windows.Controls.VirtualizingPanel.SetIsVirtualizing(itemsControl, true);
            System.Windows.Controls.VirtualizingPanel.SetVirtualizationMode(itemsControl, System.Windows.Controls.VirtualizationMode.Recycling);
            System.Windows.Controls.VirtualizingPanel.SetCacheLengthUnit(itemsControl, System.Windows.Controls.VirtualizationCacheLengthUnit.Page);
            System.Windows.Controls.VirtualizingPanel.SetCacheLength(itemsControl, new System.Windows.Controls.VirtualizationCacheLength(1, 1));
            
            // Enable container recycling
            System.Windows.Controls.VirtualizingPanel.SetIsContainerVirtualizable(itemsControl, true);
            
            // Enable UI virtualization
            System.Windows.Controls.ScrollViewer.SetCanContentScroll(itemsControl, true);
            System.Windows.Controls.ScrollViewer.SetIsDeferredScrollingEnabled(itemsControl, true);
        }
    }
}
