using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Prosim2GSX.UI.EFB.Animations
{
    /// <summary>
    /// Provides a library of standardized animations for the EFB UI.
    /// </summary>
    public static class AnimationLibrary
    {
        /// <summary>
        /// Default animation duration in milliseconds.
        /// </summary>
        public const double DefaultDuration = 300;

        /// <summary>
        /// Fast animation duration in milliseconds.
        /// </summary>
        public const double FastDuration = 150;

        /// <summary>
        /// Slow animation duration in milliseconds.
        /// </summary>
        public const double SlowDuration = 500;

        /// <summary>
        /// Creates a fade in animation.
        /// </summary>
        /// <param name="duration">The duration of the animation in milliseconds.</param>
        /// <param name="easingFunction">The easing function to use.</param>
        /// <returns>A fade in animation.</returns>
        public static DoubleAnimation CreateFadeInAnimation(double duration = DefaultDuration, IEasingFunction easingFunction = null)
        {
            var animation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easingFunction ?? new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            return animation;
        }

        /// <summary>
        /// Creates a fade out animation.
        /// </summary>
        /// <param name="duration">The duration of the animation in milliseconds.</param>
        /// <param name="easingFunction">The easing function to use.</param>
        /// <returns>A fade out animation.</returns>
        public static DoubleAnimation CreateFadeOutAnimation(double duration = DefaultDuration, IEasingFunction easingFunction = null)
        {
            var animation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easingFunction ?? new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            return animation;
        }

        /// <summary>
        /// Creates a slide in from left animation.
        /// </summary>
        /// <param name="distance">The distance to slide.</param>
        /// <param name="duration">The duration of the animation in milliseconds.</param>
        /// <param name="easingFunction">The easing function to use.</param>
        /// <returns>A slide in from left animation.</returns>
        public static ThicknessAnimation CreateSlideInFromLeftAnimation(double distance = 50, double duration = DefaultDuration, IEasingFunction easingFunction = null)
        {
            var animation = new ThicknessAnimation
            {
                From = new Thickness(-distance, 0, distance, 0),
                To = new Thickness(0),
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easingFunction ?? new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            return animation;
        }

        /// <summary>
        /// Creates a slide in from right animation.
        /// </summary>
        /// <param name="distance">The distance to slide.</param>
        /// <param name="duration">The duration of the animation in milliseconds.</param>
        /// <param name="easingFunction">The easing function to use.</param>
        /// <returns>A slide in from right animation.</returns>
        public static ThicknessAnimation CreateSlideInFromRightAnimation(double distance = 50, double duration = DefaultDuration, IEasingFunction easingFunction = null)
        {
            var animation = new ThicknessAnimation
            {
                From = new Thickness(distance, 0, -distance, 0),
                To = new Thickness(0),
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easingFunction ?? new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            return animation;
        }

        /// <summary>
        /// Creates a slide in from top animation.
        /// </summary>
        /// <param name="distance">The distance to slide.</param>
        /// <param name="duration">The duration of the animation in milliseconds.</param>
        /// <param name="easingFunction">The easing function to use.</param>
        /// <returns>A slide in from top animation.</returns>
        public static ThicknessAnimation CreateSlideInFromTopAnimation(double distance = 50, double duration = DefaultDuration, IEasingFunction easingFunction = null)
        {
            var animation = new ThicknessAnimation
            {
                From = new Thickness(0, -distance, 0, distance),
                To = new Thickness(0),
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easingFunction ?? new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            return animation;
        }

        /// <summary>
        /// Creates a slide in from bottom animation.
        /// </summary>
        /// <param name="distance">The distance to slide.</param>
        /// <param name="duration">The duration of the animation in milliseconds.</param>
        /// <param name="easingFunction">The easing function to use.</param>
        /// <returns>A slide in from bottom animation.</returns>
        public static ThicknessAnimation CreateSlideInFromBottomAnimation(double distance = 50, double duration = DefaultDuration, IEasingFunction easingFunction = null)
        {
            var animation = new ThicknessAnimation
            {
                From = new Thickness(0, distance, 0, -distance),
                To = new Thickness(0),
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easingFunction ?? new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            return animation;
        }

        /// <summary>
        /// Creates a slide out to left animation.
        /// </summary>
        /// <param name="distance">The distance to slide.</param>
        /// <param name="duration">The duration of the animation in milliseconds.</param>
        /// <param name="easingFunction">The easing function to use.</param>
        /// <returns>A slide out to left animation.</returns>
        public static ThicknessAnimation CreateSlideOutToLeftAnimation(double distance = 50, double duration = DefaultDuration, IEasingFunction easingFunction = null)
        {
            var animation = new ThicknessAnimation
            {
                From = new Thickness(0),
                To = new Thickness(-distance, 0, distance, 0),
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easingFunction ?? new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            return animation;
        }

        /// <summary>
        /// Creates a slide out to right animation.
        /// </summary>
        /// <param name="distance">The distance to slide.</param>
        /// <param name="duration">The duration of the animation in milliseconds.</param>
        /// <param name="easingFunction">The easing function to use.</param>
        /// <returns>A slide out to right animation.</returns>
        public static ThicknessAnimation CreateSlideOutToRightAnimation(double distance = 50, double duration = DefaultDuration, IEasingFunction easingFunction = null)
        {
            var animation = new ThicknessAnimation
            {
                From = new Thickness(0),
                To = new Thickness(distance, 0, -distance, 0),
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easingFunction ?? new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            return animation;
        }

        /// <summary>
        /// Creates a slide out to top animation.
        /// </summary>
        /// <param name="distance">The distance to slide.</param>
        /// <param name="duration">The duration of the animation in milliseconds.</param>
        /// <param name="easingFunction">The easing function to use.</param>
        /// <returns>A slide out to top animation.</returns>
        public static ThicknessAnimation CreateSlideOutToTopAnimation(double distance = 50, double duration = DefaultDuration, IEasingFunction easingFunction = null)
        {
            var animation = new ThicknessAnimation
            {
                From = new Thickness(0),
                To = new Thickness(0, -distance, 0, distance),
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easingFunction ?? new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            return animation;
        }

        /// <summary>
        /// Creates a slide out to bottom animation.
        /// </summary>
        /// <param name="distance">The distance to slide.</param>
        /// <param name="duration">The duration of the animation in milliseconds.</param>
        /// <param name="easingFunction">The easing function to use.</param>
        /// <returns>A slide out to bottom animation.</returns>
        public static ThicknessAnimation CreateSlideOutToBottomAnimation(double distance = 50, double duration = DefaultDuration, IEasingFunction easingFunction = null)
        {
            var animation = new ThicknessAnimation
            {
                From = new Thickness(0),
                To = new Thickness(0, distance, 0, -distance),
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easingFunction ?? new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            return animation;
        }

        /// <summary>
        /// Creates a scale in animation.
        /// </summary>
        /// <param name="fromScale">The starting scale.</param>
        /// <param name="toScale">The ending scale.</param>
        /// <param name="duration">The duration of the animation in milliseconds.</param>
        /// <param name="easingFunction">The easing function to use.</param>
        /// <returns>A scale in animation.</returns>
        public static DoubleAnimation CreateScaleAnimation(double fromScale = 0.8, double toScale = 1.0, double duration = DefaultDuration, IEasingFunction easingFunction = null)
        {
            var animation = new DoubleAnimation
            {
                From = fromScale,
                To = toScale,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easingFunction ?? new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            return animation;
        }

        /// <summary>
        /// Creates a color animation.
        /// </summary>
        /// <param name="fromColor">The starting color.</param>
        /// <param name="toColor">The ending color.</param>
        /// <param name="duration">The duration of the animation in milliseconds.</param>
        /// <param name="easingFunction">The easing function to use.</param>
        /// <returns>A color animation.</returns>
        public static ColorAnimation CreateColorAnimation(Color fromColor, Color toColor, double duration = DefaultDuration, IEasingFunction easingFunction = null)
        {
            var animation = new ColorAnimation
            {
                From = fromColor,
                To = toColor,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easingFunction ?? new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            return animation;
        }

        /// <summary>
        /// Creates a rotation animation.
        /// </summary>
        /// <param name="fromAngle">The starting angle in degrees.</param>
        /// <param name="toAngle">The ending angle in degrees.</param>
        /// <param name="duration">The duration of the animation in milliseconds.</param>
        /// <param name="easingFunction">The easing function to use.</param>
        /// <returns>A rotation animation.</returns>
        public static DoubleAnimation CreateRotationAnimation(double fromAngle = 0, double toAngle = 360, double duration = DefaultDuration, IEasingFunction easingFunction = null)
        {
            var animation = new DoubleAnimation
            {
                From = fromAngle,
                To = toAngle,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easingFunction ?? new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            return animation;
        }

        /// <summary>
        /// Creates a storyboard with a fade in animation.
        /// </summary>
        /// <param name="target">The target element.</param>
        /// <param name="duration">The duration of the animation in milliseconds.</param>
        /// <param name="easingFunction">The easing function to use.</param>
        /// <returns>A storyboard with a fade in animation.</returns>
        public static Storyboard CreateFadeInStoryboard(UIElement target, double duration = DefaultDuration, IEasingFunction easingFunction = null)
        {
            var storyboard = new Storyboard();
            var animation = CreateFadeInAnimation(duration, easingFunction);

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));

            storyboard.Children.Add(animation);

            return storyboard;
        }

        /// <summary>
        /// Creates a storyboard with a fade out animation.
        /// </summary>
        /// <param name="target">The target element.</param>
        /// <param name="duration">The duration of the animation in milliseconds.</param>
        /// <param name="easingFunction">The easing function to use.</param>
        /// <returns>A storyboard with a fade out animation.</returns>
        public static Storyboard CreateFadeOutStoryboard(UIElement target, double duration = DefaultDuration, IEasingFunction easingFunction = null)
        {
            var storyboard = new Storyboard();
            var animation = CreateFadeOutAnimation(duration, easingFunction);

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));

            storyboard.Children.Add(animation);

            return storyboard;
        }

        /// <summary>
        /// Creates a storyboard with a slide in from left animation.
        /// </summary>
        /// <param name="target">The target element.</param>
        /// <param name="distance">The distance to slide.</param>
        /// <param name="duration">The duration of the animation in milliseconds.</param>
        /// <param name="easingFunction">The easing function to use.</param>
        /// <returns>A storyboard with a slide in from left animation.</returns>
        public static Storyboard CreateSlideInFromLeftStoryboard(FrameworkElement target, double distance = 50, double duration = DefaultDuration, IEasingFunction easingFunction = null)
        {
            var storyboard = new Storyboard();
            var animation = CreateSlideInFromLeftAnimation(distance, duration, easingFunction);

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, new PropertyPath(FrameworkElement.MarginProperty));

            storyboard.Children.Add(animation);

            return storyboard;
        }

        /// <summary>
        /// Applies an animation to a target element.
        /// </summary>
        /// <param name="target">The target element.</param>
        /// <param name="animation">The animation to apply.</param>
        /// <param name="property">The property to animate.</param>
        /// <param name="completedAction">An action to execute when the animation completes.</param>
        public static void ApplyAnimation(UIElement target, AnimationTimeline animation, DependencyProperty property, Action completedAction = null)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (animation == null)
                throw new ArgumentNullException(nameof(animation));

            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (completedAction != null)
            {
                animation.Completed += (sender, e) => completedAction();
            }

            target.BeginAnimation(property, animation);
        }

        /// <summary>
        /// Applies a storyboard to a target element.
        /// </summary>
        /// <param name="storyboard">The storyboard to apply.</param>
        /// <param name="completedAction">An action to execute when the storyboard completes.</param>
        public static void ApplyStoryboard(Storyboard storyboard, Action completedAction = null)
        {
            if (storyboard == null)
                throw new ArgumentNullException(nameof(storyboard));

            if (completedAction != null)
            {
                storyboard.Completed += (sender, e) => completedAction();
            }

            storyboard.Begin();
        }
    }
}
