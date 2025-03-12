using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;

namespace Prosim2GSX.UI.EFB.Input
{
    /// <summary>
    /// Manages touch gestures for the EFB UI.
    /// </summary>
    public class TouchGestureManager
    {
        private static readonly TouchGestureManager _instance = new();
        private Point _lastTouchPoint;
        private Point _initialTouchPoint;
        private bool _isManipulating;
        private UIElement _currentElement;
        private double _initialScale = 1.0;
        private double _currentScale = 1.0;
        private const double MinScale = 0.5;
        private const double MaxScale = 3.0;
        private const double ScaleFactor = 0.01;
        private const double SwipeThreshold = 50.0;
        private const double TapThreshold = 10.0;
        private const double DoubleTapTimeThreshold = 300.0;
        private DateTime _lastTapTime = DateTime.MinValue;

        /// <summary>
        /// Gets the singleton instance of the TouchGestureManager.
        /// </summary>
        public static TouchGestureManager Instance => _instance;

        /// <summary>
        /// Initializes a new instance of the TouchGestureManager class.
        /// </summary>
        private TouchGestureManager()
        {
        }

        /// <summary>
        /// Enables touch gestures for a UIElement.
        /// </summary>
        /// <param name="element">The element to enable touch gestures for.</param>
        public void EnableTouchGestures(UIElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            // Enable manipulation events
            // Note: ManipulationMode property is not available in the current framework version
            // We'll rely on IsManipulationEnabled instead
            
            // Add event handlers
            element.ManipulationStarting += OnManipulationStarting;
            element.ManipulationStarted += OnManipulationStarted;
            element.ManipulationDelta += OnManipulationDelta;
            element.ManipulationCompleted += OnManipulationCompleted;
            element.ManipulationInertiaStarting += OnManipulationInertiaStarting;
            
            // Add touch event handlers
            element.TouchDown += OnTouchDown;
            element.TouchUp += OnTouchUp;
            element.TouchMove += OnTouchMove;
            
            // Enable touch input
            element.IsManipulationEnabled = true;
        }

        /// <summary>
        /// Disables touch gestures for a UIElement.
        /// </summary>
        /// <param name="element">The element to disable touch gestures for.</param>
        public void DisableTouchGestures(UIElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            // Remove event handlers
            element.ManipulationStarting -= OnManipulationStarting;
            element.ManipulationStarted -= OnManipulationStarted;
            element.ManipulationDelta -= OnManipulationDelta;
            element.ManipulationCompleted -= OnManipulationCompleted;
            element.ManipulationInertiaStarting -= OnManipulationInertiaStarting;
            
            // Remove touch event handlers
            element.TouchDown -= OnTouchDown;
            element.TouchUp -= OnTouchUp;
            element.TouchMove -= OnTouchMove;
            
            // Disable touch input
            element.IsManipulationEnabled = false;
        }

        /// <summary>
        /// Handles the ManipulationStarting event.
        /// </summary>
        private void OnManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            // Set the manipulation container
            e.ManipulationContainer = sender as UIElement;
            
            // Enable all manipulation modes
            e.Mode = ManipulationModes.All;
            
            // Store the current element
            _currentElement = sender as UIElement;
        }

        /// <summary>
        /// Handles the ManipulationStarted event.
        /// </summary>
        private void OnManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            _isManipulating = true;
            _initialTouchPoint = e.ManipulationOrigin;
            
            // Get the current scale if available
            if (_currentElement is FrameworkElement element && element.RenderTransform is ScaleTransform scaleTransform)
            {
                _initialScale = scaleTransform.ScaleX;
                _currentScale = _initialScale;
            }
            else
            {
                _initialScale = 1.0;
                _currentScale = 1.0;
            }
        }

        /// <summary>
        /// Handles the ManipulationDelta event.
        /// </summary>
        private void OnManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (!_isManipulating || _currentElement == null)
                return;
            
            // Handle translation
            var translation = e.DeltaManipulation.Translation;
            
            // Handle scaling
            var scale = e.DeltaManipulation.Scale;
            _currentScale = _initialScale * scale.X;
            
            // Clamp the scale
            _currentScale = Math.Max(MinScale, Math.Min(MaxScale, _currentScale));
            
            // Apply the transformation
            if (_currentElement is FrameworkElement element)
            {
                // Create or update the scale transform
                if (element.RenderTransform is ScaleTransform scaleTransform)
                {
                    scaleTransform.ScaleX = _currentScale;
                    scaleTransform.ScaleY = _currentScale;
                }
                else
                {
                    element.RenderTransform = new ScaleTransform(_currentScale, _currentScale);
                }
                
                // Set the render transform origin to the center
                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            
            // Raise the ScaleChanged event
            ScaleChanged?.Invoke(this, new ScaleChangedEventArgs(_currentScale));
            
            // Mark the event as handled
            e.Handled = true;
        }

        /// <summary>
        /// Handles the ManipulationCompleted event.
        /// </summary>
        private void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            _isManipulating = false;
            
            // Check for swipe gestures
            var totalTranslation = e.TotalManipulation.Translation;
            
            if (Math.Abs(totalTranslation.X) > SwipeThreshold)
            {
                if (totalTranslation.X > 0)
                {
                    // Swipe right
                    SwipeRight?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    // Swipe left
                    SwipeLeft?.Invoke(this, EventArgs.Empty);
                }
            }
            
            if (Math.Abs(totalTranslation.Y) > SwipeThreshold)
            {
                if (totalTranslation.Y > 0)
                {
                    // Swipe down
                    SwipeDown?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    // Swipe up
                    SwipeUp?.Invoke(this, EventArgs.Empty);
                }
            }
            
            // Mark the event as handled
            e.Handled = true;
        }

        /// <summary>
        /// Handles the ManipulationInertiaStarting event.
        /// </summary>
        private void OnManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {
            // Set deceleration for inertia
            e.TranslationBehavior.DesiredDeceleration = 10.0 * 96.0 / (1000.0 * 1000.0);
            e.ExpansionBehavior.DesiredDeceleration = 10.0 * 96.0 / (1000.0 * 1000.0);
            e.RotationBehavior.DesiredDeceleration = 10.0 * 96.0 / (1000.0 * 1000.0);
            
            // Mark the event as handled
            e.Handled = true;
        }

        /// <summary>
        /// Handles the TouchDown event.
        /// </summary>
        private void OnTouchDown(object sender, TouchEventArgs e)
        {
            _lastTouchPoint = e.GetTouchPoint(null).Position;
            _initialTouchPoint = _lastTouchPoint;
            
            // Check for double tap
            var now = DateTime.Now;
            if ((now - _lastTapTime).TotalMilliseconds < DoubleTapTimeThreshold)
            {
                // Double tap detected
                DoubleTap?.Invoke(this, new TouchEventArgs(e.TouchDevice, e.Timestamp));
            }
            
            _lastTapTime = now;
            
            // Mark the event as handled
            e.Handled = true;
        }

        /// <summary>
        /// Handles the TouchUp event.
        /// </summary>
        private void OnTouchUp(object sender, TouchEventArgs e)
        {
            var currentPoint = e.GetTouchPoint(null).Position;
            
            // Check for tap
            if (Distance(_initialTouchPoint, currentPoint) < TapThreshold)
            {
                // Tap detected
                Tap?.Invoke(this, new TouchEventArgs(e.TouchDevice, e.Timestamp));
            }
            
            // Mark the event as handled
            e.Handled = true;
        }

        /// <summary>
        /// Handles the TouchMove event.
        /// </summary>
        private void OnTouchMove(object sender, TouchEventArgs e)
        {
            var currentPoint = e.GetTouchPoint(null).Position;
            
            // Calculate the delta
            var delta = new Point(currentPoint.X - _lastTouchPoint.X, currentPoint.Y - _lastTouchPoint.Y);
            
            // Update the last touch point
            _lastTouchPoint = currentPoint;
            
            // Raise the TouchMove event
            TouchMove?.Invoke(this, new TouchMoveEventArgs(delta, e.TouchDevice, e.Timestamp));
            
            // Mark the event as handled
            e.Handled = true;
        }

        /// <summary>
        /// Calculates the distance between two points.
        /// </summary>
        private double Distance(Point p1, Point p2)
        {
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Occurs when a tap gesture is detected.
        /// </summary>
        public event EventHandler<TouchEventArgs> Tap;

        /// <summary>
        /// Occurs when a double tap gesture is detected.
        /// </summary>
        public event EventHandler<TouchEventArgs> DoubleTap;

        /// <summary>
        /// Occurs when a swipe left gesture is detected.
        /// </summary>
        public event EventHandler SwipeLeft;

        /// <summary>
        /// Occurs when a swipe right gesture is detected.
        /// </summary>
        public event EventHandler SwipeRight;

        /// <summary>
        /// Occurs when a swipe up gesture is detected.
        /// </summary>
        public event EventHandler SwipeUp;

        /// <summary>
        /// Occurs when a swipe down gesture is detected.
        /// </summary>
        public event EventHandler SwipeDown;

        /// <summary>
        /// Occurs when the scale changes during a pinch gesture.
        /// </summary>
        public event EventHandler<ScaleChangedEventArgs> ScaleChanged;

        /// <summary>
        /// Occurs when a touch move gesture is detected.
        /// </summary>
        public event EventHandler<TouchMoveEventArgs> TouchMove;
    }

    /// <summary>
    /// Provides data for the ScaleChanged event.
    /// </summary>
    public class ScaleChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the new scale.
        /// </summary>
        public double Scale { get; }

        /// <summary>
        /// Initializes a new instance of the ScaleChangedEventArgs class.
        /// </summary>
        /// <param name="scale">The new scale.</param>
        public ScaleChangedEventArgs(double scale)
        {
            Scale = scale;
        }
    }

    /// <summary>
    /// Provides data for the TouchMove event.
    /// </summary>
    public class TouchMoveEventArgs : TouchEventArgs
    {
        /// <summary>
        /// Gets the delta movement.
        /// </summary>
        public Point Delta { get; }

        /// <summary>
        /// Initializes a new instance of the TouchMoveEventArgs class.
        /// </summary>
        /// <param name="delta">The delta movement.</param>
        /// <param name="touchDevice">The touch device.</param>
        /// <param name="timestamp">The timestamp.</param>
        public TouchMoveEventArgs(Point delta, TouchDevice touchDevice, int timestamp)
            : base(touchDevice, timestamp)
        {
            Delta = delta;
        }
    }
}
