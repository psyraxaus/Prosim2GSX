using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Prosim2GSX.UI.EFB.Views.Aircraft
{
    /// <summary>
    /// Enum representing the type of door.
    /// </summary>
    public enum DoorType
    {
        Passenger,
        Cargo
    }

    /// <summary>
    /// Class representing a door indicator.
    /// </summary>
    public class DoorIndicator
    {
        public Grid Container { get; set; }
        public Rectangle Background { get; set; }
        public Path Symbol { get; set; }
        public DoorType Type { get; set; }
    }

    /// <summary>
    /// Class representing a refueling point.
    /// </summary>
    public class RefuelingPoint
    {
        public Grid Container { get; set; }
        public Ellipse Background { get; set; }
        public Path Symbol { get; set; }
    }

    /// <summary>
    /// Control for displaying an aircraft diagram with interactive elements.
    /// </summary>
    public class AircraftDiagramControl : UserControl
    {
        private readonly Dictionary<string, DoorIndicator> _doorIndicators = new Dictionary<string, DoorIndicator>();
        private RefuelingPoint _refuelingPoint;
        private Canvas _mainCanvas;
        private Canvas _cabinLayout;
        private Canvas _doorIndicatorsCanvas;
        private Canvas _servicePointsCanvas;
        private Path _aircraftOutline;

        // Theme-aware brushes
        private Brush _aircraftFill;
        private Brush _aircraftStroke;
        private Brush _seatFill;
        private Brush _seatStroke;
        private Brush _galleryFill;
        private Brush _galleryStroke;
        private Brush _lavatoryFill;
        private Brush _lavatoryStroke;
        
        // Door colors
        private Brush _doorClosedFill;
        private Brush _doorClosedStroke;
        private Brush _doorClosedSymbolFill;
        private Brush _doorClosedSymbolStroke;
        private Brush _doorOpenFill;
        private Brush _doorOpenStroke;
        private Brush _doorOpenSymbolFill;
        private Brush _doorOpenSymbolStroke;
        
        // Refueling colors
        private Brush _refuelingInactiveFill;
        private Brush _refuelingInactiveStroke;
        private Brush _refuelingActiveFill;
        private Brush _refuelingActiveStroke;
        private Brush _refuelingSymbolStroke;

        /// <summary>
        /// Initializes a new instance of the <see cref="AircraftDiagramControl"/> class.
        /// </summary>
        public AircraftDiagramControl()
        {
            // Initialize default brushes
            InitializeBrushes();
            
            // Subscribe to theme changed events
            if (Application.Current.Resources.Contains("EFBThemeManager"))
            {
                var themeManager = Application.Current.Resources["EFBThemeManager"] as Prosim2GSX.UI.EFB.Themes.EFBThemeManager;
                if (themeManager != null)
                {
                    themeManager.ThemeChanged += OnThemeChanged;
                }
            }
            
            // Initialize the component
            InitializeComponent();
        }

        /// <summary>
        /// Initializes the default brushes.
        /// </summary>
        private void InitializeBrushes()
        {
            // Get theme information
            bool isDarkTheme = Application.Current.Resources.Contains("isDarkTheme") && 
                              (bool)Application.Current.Resources["isDarkTheme"];
            
            // Set aircraft colors based on theme
            if (isDarkTheme)
            {
                _aircraftFill = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                _aircraftStroke = new SolidColorBrush(Colors.White);
                _seatFill = new SolidColorBrush(Color.FromRgb(150, 150, 150));
                _seatStroke = new SolidColorBrush(Colors.White);
            }
            else
            {
                _aircraftFill = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                _aircraftStroke = new SolidColorBrush(Colors.Black);
                _seatFill = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                _seatStroke = new SolidColorBrush(Colors.Black);
            }
            
            // Get colors from theme
            var primaryColor = GetResourceColor("EFBPrimaryColor", Colors.DodgerBlue);
            var accentColor = GetResourceColor("EFBAccentColor", Colors.Orange);
            var backgroundColor = GetResourceColor("EFBBackgroundColor", Colors.White);
            var foregroundColor = GetResourceColor("EFBForegroundColor", Colors.Black);
            var successColor = GetResourceColor("EFBSuccessColor", Colors.Green);
            var warningColor = GetResourceColor("EFBWarningColor", Colors.Orange);
            
            // Set galley and lavatory colors
            _galleryFill = new SolidColorBrush(Color.FromArgb(150, primaryColor.R, primaryColor.G, primaryColor.B));
            _galleryStroke = new SolidColorBrush(primaryColor);
            _lavatoryFill = new SolidColorBrush(Color.FromArgb(150, accentColor.R, accentColor.G, accentColor.B));
            _lavatoryStroke = new SolidColorBrush(accentColor);
            
            // Set door colors
            _doorClosedFill = new SolidColorBrush(backgroundColor);
            _doorClosedStroke = new SolidColorBrush(foregroundColor);
            _doorClosedSymbolFill = new SolidColorBrush(foregroundColor);
            _doorClosedSymbolStroke = new SolidColorBrush(foregroundColor);
            
            _doorOpenFill = new SolidColorBrush(successColor);
            _doorOpenStroke = new SolidColorBrush(foregroundColor);
            _doorOpenSymbolFill = new SolidColorBrush(backgroundColor);
            _doorOpenSymbolStroke = new SolidColorBrush(backgroundColor);
            
            // Set refueling colors
            _refuelingInactiveFill = new SolidColorBrush(backgroundColor);
            _refuelingInactiveStroke = new SolidColorBrush(foregroundColor);
            _refuelingSymbolStroke = new SolidColorBrush(foregroundColor);
            
            _refuelingActiveFill = new SolidColorBrush(warningColor);
            _refuelingActiveStroke = new SolidColorBrush(foregroundColor);
        }

        /// <summary>
        /// Gets a color resource with a fallback.
        /// </summary>
        /// <param name="resourceKey">The resource key.</param>
        /// <param name="defaultColor">The default color.</param>
        /// <returns>The color resource or fallback.</returns>
        private Color GetResourceColor(string resourceKey, Color defaultColor)
        {
            if (Application.Current.Resources.Contains(resourceKey))
            {
                return (Color)Application.Current.Resources[resourceKey];
            }
            return defaultColor;
        }

        /// <summary>
        /// Handles theme changed events.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void OnThemeChanged(object sender, EventArgs e)
        {
            // Update brushes
            InitializeBrushes();
            
            // Update visual elements
            UpdateVisualElements();
        }

        /// <summary>
        /// Updates all visual elements based on the current brushes.
        /// </summary>
        private void UpdateVisualElements()
        {
            // Update aircraft outline
            if (_aircraftOutline != null)
            {
                _aircraftOutline.Fill = _aircraftFill;
                _aircraftOutline.Stroke = _aircraftStroke;
            }
            
            // Update seats
            if (_cabinLayout != null)
            {
                foreach (var child in _cabinLayout.Children)
                {
                    if (child is Rectangle rect)
                    {
                        // Check if it's a seat, galley, or lavatory based on size
                        if (rect.Width == 6 && rect.Height == 6)
                        {
                            // It's a seat
                            rect.Fill = _seatFill;
                            rect.Stroke = _seatStroke;
                        }
                        else if (rect.Width == 20 && rect.Height == 15)
                        {
                            // It's a galley
                            rect.Fill = _galleryFill;
                            rect.Stroke = _galleryStroke;
                        }
                        else if (rect.Width == 15 && rect.Height == 15)
                        {
                            // It's a lavatory
                            rect.Fill = _lavatoryFill;
                            rect.Stroke = _lavatoryStroke;
                        }
                    }
                }
            }
            
            // Update door indicators
            foreach (var doorPair in _doorIndicators)
            {
                var doorIndicator = doorPair.Value;
                bool isOpen = false;
                
                // Determine if door is open based on the current fill
                if (doorIndicator.Background.Fill is SolidColorBrush brush && 
                    brush.Color == ((SolidColorBrush)_doorOpenFill).Color)
                {
                    isOpen = true;
                }
                
                // Update colors based on state
                doorIndicator.Background.Fill = isOpen ? _doorOpenFill : _doorClosedFill;
                doorIndicator.Background.Stroke = isOpen ? _doorOpenStroke : _doorClosedStroke;
                doorIndicator.Symbol.Fill = isOpen ? _doorOpenSymbolFill : _doorClosedSymbolFill;
                doorIndicator.Symbol.Stroke = isOpen ? _doorOpenSymbolStroke : _doorClosedSymbolStroke;
            }
            
            // Update refueling point
            if (_refuelingPoint != null)
            {
                bool isActive = false;
                
                // Determine if refueling is active based on the current fill
                if (_refuelingPoint.Background.Fill is SolidColorBrush brush && 
                    brush.Color == ((SolidColorBrush)_refuelingActiveFill).Color)
                {
                    isActive = true;
                }
                
                // Update colors based on state
                _refuelingPoint.Background.Fill = isActive ? _refuelingActiveFill : _refuelingInactiveFill;
                _refuelingPoint.Background.Stroke = isActive ? _refuelingActiveStroke : _refuelingInactiveStroke;
                _refuelingPoint.Symbol.Stroke = _refuelingSymbolStroke;
            }
        }

        /// <summary>
        /// Initializes the component.
        /// </summary>
        private void InitializeComponent()
        {
            // Create a grid for the aircraft diagram
            var grid = new Grid();
            
            // Create canvases for different layers
            _mainCanvas = new Canvas
            {
                Width = 600,
                Height = 400,
                Background = Brushes.Transparent
            };
            
            _cabinLayout = new Canvas
            {
                Width = 600,
                Height = 400,
                Background = Brushes.Transparent
            };
            
            _doorIndicatorsCanvas = new Canvas
            {
                Width = 600,
                Height = 400,
                Background = Brushes.Transparent
            };
            
            _servicePointsCanvas = new Canvas
            {
                Width = 600,
                Height = 400,
                Background = Brushes.Transparent
            };
            
            // Add the aircraft outline
            _aircraftOutline = new Path
            {
                // A320 silhouette path data (top view, facing up)
                Data = Geometry.Parse("M 300,50 C 320,50 340,55 350,65 L 400,120 L 450,120 C 470,120 480,130 480,150 L 480,250 C 480,270 470,280 450,280 L 400,280 L 350,335 C 340,345 320,350 300,350 C 280,350 260,345 250,335 L 200,280 L 150,280 C 130,280 120,270 120,250 L 120,150 C 120,130 130,120 150,120 L 200,120 L 250,65 C 260,55 280,50 300,50 Z"),
                Fill = _aircraftFill,
                Stroke = _aircraftStroke,
                StrokeThickness = 2
            };
            _mainCanvas.Children.Add(_aircraftOutline);
            
            // Add wings
            var leftWing = new Path
            {
                Data = Geometry.Parse("M 200,180 L 100,220 L 100,240 L 200,220 Z"),
                Fill = _aircraftFill,
                Stroke = _aircraftStroke,
                StrokeThickness = 2
            };
            _mainCanvas.Children.Add(leftWing);
            
            var rightWing = new Path
            {
                Data = Geometry.Parse("M 400,180 L 500,220 L 500,240 L 400,220 Z"),
                Fill = _aircraftFill,
                Stroke = _aircraftStroke,
                StrokeThickness = 2
            };
            _mainCanvas.Children.Add(rightWing);
            
            // Add engines
            var leftEngine = new Path
            {
                Data = Geometry.Parse("M 150,210 C 150,200 160,195 170,195 L 190,195 C 200,195 210,200 210,210 C 210,220 200,225 190,225 L 170,225 C 160,225 150,220 150,210 Z"),
                Fill = _aircraftFill,
                Stroke = _aircraftStroke,
                StrokeThickness = 1.5
            };
            _mainCanvas.Children.Add(leftEngine);
            
            var rightEngine = new Path
            {
                Data = Geometry.Parse("M 390,210 C 390,200 400,195 410,195 L 430,195 C 440,195 450,200 450,210 C 450,220 440,225 430,225 L 410,225 C 400,225 390,220 390,210 Z"),
                Fill = _aircraftFill,
                Stroke = _aircraftStroke,
                StrokeThickness = 1.5
            };
            _mainCanvas.Children.Add(rightEngine);
            
            // Generate seatmap
            GenerateSeatmap();
            
            // Create door indicators
            CreateDoorIndicator(_doorIndicatorsCanvas, "ForwardLeft", 180, 130, DoorType.Passenger);
            CreateDoorIndicator(_doorIndicatorsCanvas, "ForwardRight", 420, 130, DoorType.Passenger);
            CreateDoorIndicator(_doorIndicatorsCanvas, "AftLeft", 180, 270, DoorType.Passenger);
            CreateDoorIndicator(_doorIndicatorsCanvas, "AftRight", 420, 270, DoorType.Passenger);
            CreateDoorIndicator(_doorIndicatorsCanvas, "ForwardCargo", 220, 240, DoorType.Cargo);
            CreateDoorIndicator(_doorIndicatorsCanvas, "AftCargo", 380, 240, DoorType.Cargo);
            
            // Create refueling point
            CreateRefuelingPoint(_servicePointsCanvas);
            
            // Add all canvases to the grid
            grid.Children.Add(_mainCanvas);
            grid.Children.Add(_cabinLayout);
            grid.Children.Add(_doorIndicatorsCanvas);
            grid.Children.Add(_servicePointsCanvas);
            
            // Set the content of this UserControl to the grid
            Content = grid;
        }

        /// <summary>
        /// Generates the seatmap.
        /// </summary>
        private void GenerateSeatmap()
        {
            // A320 typically has 6 seats per row (3-3 configuration)
            const int seatsPerRow = 6;
            const int maxRows = 22; // To accommodate up to 132 seats
            
            // Clear existing seats
            _cabinLayout.Children.Clear();
            
            // Create seat grid
            for (int row = 0; row < maxRows; row++)
            {
                for (int seat = 0; seat < seatsPerRow; seat++)
                {
                    // Create seat rectangle
                    var seatRect = new Rectangle
                    {
                        Width = 6,
                        Height = 6,
                        Fill = _seatFill,
                        Stroke = _seatStroke,
                        StrokeThickness = 0.5,
                        RadiusX = 1,
                        RadiusY = 1
                    };
                    
                    // Position seat
                    double xOffset = seat < 3 ? -15 - (seat * 8) : 15 + ((seat - 3) * 8);
                    Canvas.SetLeft(seatRect, 300 + xOffset); // Center of aircraft
                    Canvas.SetTop(seatRect, 120 + (row * 10)); // Starting from front
                    
                    // Add seat to layout
                    _cabinLayout.Children.Add(seatRect);
                }
            }
            
            // Add galleys and lavatories
            AddGalley(280, 90); // Forward galley
            AddGalley(280, 310); // Aft galley
            AddLavatory(320, 310); // Aft lavatory
        }

        /// <summary>
        /// Adds a galley to the cabin layout.
        /// </summary>
        /// <param name="left">The left position.</param>
        /// <param name="top">The top position.</param>
        private void AddGalley(double left, double top)
        {
            var galley = new Rectangle
            {
                Width = 40,
                Height = 15,
                Fill = _galleryFill,
                Stroke = _galleryStroke,
                StrokeThickness = 0.5
            };
            
            Canvas.SetLeft(galley, left);
            Canvas.SetTop(galley, top);
            _cabinLayout.Children.Add(galley);
        }

        /// <summary>
        /// Adds a lavatory to the cabin layout.
        /// </summary>
        /// <param name="left">The left position.</param>
        /// <param name="top">The top position.</param>
        private void AddLavatory(double left, double top)
        {
            var lavatory = new Rectangle
            {
                Width = 15,
                Height = 15,
                Fill = _lavatoryFill,
                Stroke = _lavatoryStroke,
                StrokeThickness = 0.5,
                RadiusX = 2,
                RadiusY = 2
            };
            
            Canvas.SetLeft(lavatory, left);
            Canvas.SetTop(lavatory, top);
            _cabinLayout.Children.Add(lavatory);
        }

        /// <summary>
        /// Creates a door indicator.
        /// </summary>
        /// <param name="canvas">The canvas to add the door to.</param>
        /// <param name="doorName">The name of the door.</param>
        /// <param name="left">The left position.</param>
        /// <param name="top">The top position.</param>
        /// <param name="doorType">The type of door.</param>
        private void CreateDoorIndicator(Canvas canvas, string doorName, double left, double top, DoorType doorType)
        {
            // Create door container with transform for animation
            var doorContainer = new Grid
            {
                RenderTransform = new ScaleTransform(1, 1),
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            Canvas.SetLeft(doorContainer, left);
            Canvas.SetTop(doorContainer, top);
            
            // Create door background
            var doorBackground = new Rectangle
            {
                Width = doorType == DoorType.Passenger ? 20 : 30,
                Height = doorType == DoorType.Passenger ? 20 : 10,
                Fill = _doorClosedFill,
                Stroke = _doorClosedStroke,
                StrokeThickness = 1,
                RadiusX = 2,
                RadiusY = 2
            };
            
            // Create door symbol (closed by default)
            var doorSymbol = new Path
            {
                Data = doorType == DoorType.Passenger 
                    ? Geometry.Parse("M 2,2 L 18,2 L 18,18 L 2,18 Z") // Closed passenger door
                    : Geometry.Parse("M 2,2 L 28,2 L 28,8 L 2,8 Z"),  // Closed cargo door
                Fill = _doorClosedSymbolFill,
                Stroke = _doorClosedSymbolStroke,
                StrokeThickness = 1
            };
            
            // Add elements to container
            doorContainer.Children.Add(doorBackground);
            doorContainer.Children.Add(doorSymbol);
            
            // Add to canvas
            canvas.Children.Add(doorContainer);
            
            // Store reference
            _doorIndicators[doorName] = new DoorIndicator
            {
                Container = doorContainer,
                Background = doorBackground,
                Symbol = doorSymbol,
                Type = doorType
            };
            
            // Add label
            var label = new TextBlock
            {
                Text = doorName,
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                Width = doorType == DoorType.Passenger ? 20 : 30,
                Foreground = _doorClosedSymbolFill
            };
            Canvas.SetLeft(label, left);
            Canvas.SetTop(label, top - 15);
            canvas.Children.Add(label);
        }

        /// <summary>
        /// Creates a refueling point.
        /// </summary>
        /// <param name="canvas">The canvas to add the refueling point to.</param>
        private void CreateRefuelingPoint(Canvas canvas)
        {
            // Create refueling point container with transform for animation
            var refuelingContainer = new Grid
            {
                RenderTransform = new ScaleTransform(1, 1),
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            Canvas.SetLeft(refuelingContainer, 450);
            Canvas.SetTop(refuelingContainer, 230);
            
            // Create refueling point background
            var refuelingBackground = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = _refuelingInactiveFill,
                Stroke = _refuelingInactiveStroke,
                StrokeThickness = 1
            };
            
            // Create refueling symbol
            var refuelingSymbol = new Path
            {
                Data = Geometry.Parse("M 5,10 L 15,10 M 10,5 L 10,15"), // Fuel symbol (+)
                Stroke = _refuelingSymbolStroke,
                StrokeThickness = 2
            };
            
            // Add elements to container
            refuelingContainer.Children.Add(refuelingBackground);
            refuelingContainer.Children.Add(refuelingSymbol);
            
            // Add to canvas
            canvas.Children.Add(refuelingContainer);
            
            // Store reference
            _refuelingPoint = new RefuelingPoint
            {
                Container = refuelingContainer,
                Background = refuelingBackground,
                Symbol = refuelingSymbol
            };
            
            // Add label
            var label = new TextBlock
            {
                Text = "Refueling",
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                Width = 60,
                Foreground = _refuelingSymbolStroke
            };
            Canvas.SetLeft(label, 430);
            Canvas.SetTop(label, 250);
            canvas.Children.Add(label);
        }

        /// <summary>
        /// Sets a door as active (open) on the aircraft diagram.
        /// </summary>
        /// <param name="doorName">The name of the door to set as active.</param>
        /// <param name="isActive">Whether the door is active (open).</param>
        public void SetDoorActive(string doorName, bool isActive)
        {
            if (_doorIndicators.TryGetValue(doorName, out var doorIndicator))
            {
                // Update door symbol based on state
                if (isActive)
                {
                    // Open door symbol - different for passenger vs cargo
                    if (doorIndicator.Type == DoorType.Passenger)
                    {
                        doorIndicator.Symbol.Data = Geometry.Parse("M 2,2 L 18,2 L 18,10 L 10,18 L 2,18 Z"); // Open passenger door
                    }
                    else
                    {
                        doorIndicator.Symbol.Data = Geometry.Parse("M 2,2 L 28,2 L 28,4 L 15,8 L 2,4 Z"); // Open cargo door
                    }
                    
                    doorIndicator.Background.Fill = _doorOpenFill;
                    doorIndicator.Symbol.Fill = _doorOpenSymbolFill;
                }
                else
                {
                    // Closed door symbol
                    if (doorIndicator.Type == DoorType.Passenger)
                    {
                        doorIndicator.Symbol.Data = Geometry.Parse("M 2,2 L 18,2 L 18,18 L 2,18 Z"); // Closed passenger door
                    }
                    else
                    {
                        doorIndicator.Symbol.Data = Geometry.Parse("M 2,2 L 28,2 L 28,8 L 2,8 Z"); // Closed cargo door
                    }
                    
                    doorIndicator.Background.Fill = _doorClosedFill;
                    doorIndicator.Symbol.Fill = _doorClosedSymbolFill;
                }
                
                // Animate the transition
                AnimateDoorState(doorIndicator, isActive);
            }
        }

        /// <summary>
        /// Animates the door state change.
        /// </summary>
        /// <param name="doorIndicator">The door indicator.</param>
        /// <param name="isOpen">Whether the door is open.</param>
        private void AnimateDoorState(DoorIndicator doorIndicator, bool isOpen)
        {
            // Create animation for background color
            var backgroundAnimation = new ColorAnimation
            {
                To = isOpen ? ((SolidColorBrush)_doorOpenFill).Color : ((SolidColorBrush)_doorClosedFill).Color,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            
            // Create animation for symbol color
            var symbolAnimation = new ColorAnimation
            {
                To = isOpen ? ((SolidColorBrush)_doorOpenSymbolFill).Color : ((SolidColorBrush)_doorClosedSymbolFill).Color,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            
            // Create scale animation for container
            var scaleAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.2,
                Duration = TimeSpan.FromMilliseconds(150),
                AutoReverse = true
            };
            
            // Apply animations
            ((SolidColorBrush)doorIndicator.Background.Fill).BeginAnimation(SolidColorBrush.ColorProperty, backgroundAnimation);
            ((SolidColorBrush)doorIndicator.Symbol.Fill).BeginAnimation(SolidColorBrush.ColorProperty, symbolAnimation);
            ((ScaleTransform)doorIndicator.Container.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            ((ScaleTransform)doorIndicator.Container.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
        }

        /// <summary>
        /// Sets the refueling point as active.
        /// </summary>
        /// <param name="isActive">Whether refueling is active.</param>
        public void SetRefuelingActive(bool isActive)
        {
            if (_refuelingPoint != null)
            {
                // Update refueling point based on state
                if (isActive)
                {
                    _refuelingPoint.Background.Fill = _refuelingActiveFill;
                    _refuelingPoint.Background.Stroke = _refuelingActiveStroke;
                    
                    // Start pulsing animation for active refueling
                    StartRefuelingAnimation();
                }
                else
                {
                    _refuelingPoint.Background.Fill = _refuelingInactiveFill;
                    _refuelingPoint.Background.Stroke = _refuelingInactiveStroke;
                    
                    // Stop animation
                    StopRefuelingAnimation();
                }
            }
        }

        /// <summary>
        /// Starts the refueling animation.
        /// </summary>
        private void StartRefuelingAnimation()
        {
            // Create pulsing animation
            var animation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.3,
                Duration = TimeSpan.FromSeconds(1),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            
            // Apply animation to refueling point
            ((ScaleTransform)_refuelingPoint.Container.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            ((ScaleTransform)_refuelingPoint.Container.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        /// <summary>
        /// Stops the refueling animation.
        /// </summary>
        private void StopRefuelingAnimation()
        {
            // Stop animations
            ((ScaleTransform)_refuelingPoint.Container.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, null);
            ((ScaleTransform)_refuelingPoint.Container.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, null);
        }

        /// <summary>
        /// Highlights a door on the aircraft diagram.
        /// </summary>
        /// <param name="doorName">The name of the door to highlight.</param>
        public void HighlightDoor(string doorName)
        {
            if (_doorIndicators.TryGetValue(doorName, out var doorIndicator))
            {
                // Create scale animation for container
                var scaleAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.3,
                    Duration = TimeSpan.FromMilliseconds(300),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                
                // Apply animation
                ((ScaleTransform)doorIndicator.Container.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                ((ScaleTransform)doorIndicator.Container.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            }
        }

        /// <summary>
        /// Resets all door highlights on the aircraft diagram.
        /// </summary>
        public void ResetDoorHighlights()
        {
            foreach (var doorPair in _doorIndicators)
            {
                var doorIndicator = doorPair.Value;
                
                // Stop animations
                ((ScaleTransform)doorIndicator.Container.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, null);
                ((ScaleTransform)doorIndicator.Container.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, null);
            }
        }

        /// <summary>
        /// Highlights a service point on the aircraft diagram.
        /// </summary>
        /// <param name="servicePointName">The name of the service point to highlight.</param>
        public void HighlightServicePoint(string servicePointName)
        {
            if (servicePointName == "Refueling" && _refuelingPoint != null)
            {
                // Create scale animation for container
                var scaleAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.3,
                    Duration = TimeSpan.FromMilliseconds(300),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(3)
                };
                
                // Apply animation
                ((ScaleTransform)_refuelingPoint.Container.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                ((ScaleTransform)_refuelingPoint.Container.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            }
        }

        /// <summary>
        /// Resets all service point highlights on the aircraft diagram.
        /// </summary>
        public void ResetServicePointHighlights()
        {
            if (_refuelingPoint != null)
            {
                // Stop animations if not active
                if (_refuelingPoint.Background.Fill is SolidColorBrush brush && 
                    brush.Color != ((SolidColorBrush)_refuelingActiveFill).Color)
                {
                    ((ScaleTransform)_refuelingPoint.Container.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    ((ScaleTransform)_refuelingPoint.Container.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, null);
                }
            }
        }
    }
}
