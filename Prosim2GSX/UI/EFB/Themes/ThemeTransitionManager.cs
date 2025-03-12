using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace Prosim2GSX.UI.EFB.Themes
{
    /// <summary>
    /// Manages transitions between themes.
    /// </summary>
    public class ThemeTransitionManager
    {
        private static readonly TimeSpan DefaultTransitionDuration = TimeSpan.FromMilliseconds(300);
        private static readonly double DefaultFadeOutOpacity = 0.8;
        private static readonly double DefaultFadeInOpacity = 1.0;

        /// <summary>
        /// Begins a theme transition by fading out the UI.
        /// </summary>
        /// <param name="element">The element to animate.</param>
        /// <param name="duration">The duration of the animation.</param>
        public static void BeginTransition(UIElement element, TimeSpan? duration = null)
        {
            if (element == null)
            {
                return;
            }

            var fadeOut = new DoubleAnimation
            {
                From = DefaultFadeInOpacity,
                To = DefaultFadeOutOpacity,
                Duration = duration ?? DefaultTransitionDuration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            element.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        /// <summary>
        /// Completes a theme transition by fading in the UI.
        /// </summary>
        /// <param name="element">The element to animate.</param>
        /// <param name="duration">The duration of the animation.</param>
        public static void CompleteTransition(UIElement element, TimeSpan? duration = null)
        {
            if (element == null)
            {
                return;
            }

            var fadeIn = new DoubleAnimation
            {
                From = DefaultFadeOutOpacity,
                To = DefaultFadeInOpacity,
                Duration = duration ?? DefaultTransitionDuration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            element.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        /// <summary>
        /// Performs a theme transition with a callback in between.
        /// </summary>
        /// <param name="element">The element to animate.</param>
        /// <param name="transitionAction">The action to perform during the transition.</param>
        /// <param name="duration">The duration of each phase of the animation.</param>
        public static void PerformTransition(UIElement element, Action transitionAction, TimeSpan? duration = null)
        {
            if (element == null || transitionAction == null)
            {
                return;
            }

            var actualDuration = duration ?? DefaultTransitionDuration;

            // Create the fade out animation
            var fadeOut = new DoubleAnimation
            {
                From = DefaultFadeInOpacity,
                To = DefaultFadeOutOpacity,
                Duration = actualDuration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            // Create the fade in animation
            var fadeIn = new DoubleAnimation
            {
                From = DefaultFadeOutOpacity,
                To = DefaultFadeInOpacity,
                Duration = actualDuration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            // When the fade out completes, perform the action and start the fade in
            fadeOut.Completed += (s, e) =>
            {
                try
                {
                    transitionAction();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error during theme transition: {ex.Message}");
                }
                finally
                {
                    element.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                }
            };

            // Start the fade out
            element.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
    }
}
