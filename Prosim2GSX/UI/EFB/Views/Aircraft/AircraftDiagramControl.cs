using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Prosim2GSX.UI.EFB.Views.Aircraft
{
    /// <summary>
    /// Control for displaying an aircraft diagram with interactive elements.
    /// </summary>
    public class AircraftDiagramControl : UserControl
    {
        private readonly Dictionary<string, Shape> _doorShapes = new Dictionary<string, Shape>();
        private readonly Dictionary<string, Shape> _servicePointShapes = new Dictionary<string, Shape>();
        private readonly Brush _defaultFill = Brushes.LightGray;
        private readonly Brush _highlightFill = Brushes.Yellow;
        private readonly Brush _activeFill = Brushes.Green;

        /// <summary>
        /// Initializes a new instance of the <see cref="AircraftDiagramControl"/> class.
        /// </summary>
        public AircraftDiagramControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes the component.
        /// </summary>
        private void InitializeComponent()
        {
            // Create a canvas for the aircraft diagram
            var canvas = new Canvas
            {
                Width = 600,
                Height = 300,
                Background = Brushes.White
            };

            // Create the aircraft fuselage
            var fuselage = new Rectangle
            {
                Width = 500,
                Height = 80,
                Fill = Brushes.LightGray,
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                RadiusX = 40,
                RadiusY = 40
            };
            Canvas.SetLeft(fuselage, 50);
            Canvas.SetTop(fuselage, 110);
            canvas.Children.Add(fuselage);

            // Create the aircraft wings
            var leftWing = new Rectangle
            {
                Width = 200,
                Height = 30,
                Fill = Brushes.LightGray,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            Canvas.SetLeft(leftWing, 150);
            Canvas.SetTop(leftWing, 190);
            canvas.Children.Add(leftWing);

            var rightWing = new Rectangle
            {
                Width = 200,
                Height = 30,
                Fill = Brushes.LightGray,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            Canvas.SetLeft(rightWing, 150);
            Canvas.SetTop(rightWing, 80);
            canvas.Children.Add(rightWing);

            // Create the aircraft tail
            var tail = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(550, 110),
                    new Point(600, 80),
                    new Point(600, 220),
                    new Point(550, 190)
                },
                Fill = Brushes.LightGray,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            canvas.Children.Add(tail);

            // Create the aircraft nose
            var nose = new Ellipse
            {
                Width = 40,
                Height = 80,
                Fill = Brushes.LightGray,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            Canvas.SetLeft(nose, 30);
            Canvas.SetTop(nose, 110);
            canvas.Children.Add(nose);

            // Create the doors
            CreateDoor(canvas, "ForwardLeft", 100, 110, 20, 20);
            CreateDoor(canvas, "ForwardRight", 100, 170, 20, 20);
            CreateDoor(canvas, "AftLeft", 400, 110, 20, 20);
            CreateDoor(canvas, "AftRight", 400, 170, 20, 20);
            CreateDoor(canvas, "ForwardCargo", 150, 170, 30, 10);
            CreateDoor(canvas, "AftCargo", 350, 170, 30, 10);

            // Create the service points
            CreateServicePoint(canvas, "Refueling", 250, 190, 20, 20);
            CreateServicePoint(canvas, "Catering", 300, 110, 20, 20);
            CreateServicePoint(canvas, "Boarding", 200, 110, 20, 20);
            CreateServicePoint(canvas, "Cargo", 250, 170, 20, 20);

            // Set the content of this UserControl to the canvas
            Content = canvas;
        }

        /// <summary>
        /// Creates a door shape on the canvas.
        /// </summary>
        /// <param name="canvas">The canvas to add the door to.</param>
        /// <param name="doorName">The name of the door.</param>
        /// <param name="left">The left position of the door.</param>
        /// <param name="top">The top position of the door.</param>
        /// <param name="width">The width of the door.</param>
        /// <param name="height">The height of the door.</param>
        private void CreateDoor(Canvas canvas, string doorName, double left, double top, double width, double height)
        {
            var door = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = _defaultFill,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Tag = doorName
            };
            Canvas.SetLeft(door, left);
            Canvas.SetTop(door, top);
            canvas.Children.Add(door);
            _doorShapes[doorName] = door;

            // Add a label for the door
            var label = new TextBlock
            {
                Text = doorName,
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                Width = width
            };
            Canvas.SetLeft(label, left);
            Canvas.SetTop(label, top - 15);
            canvas.Children.Add(label);
        }

        /// <summary>
        /// Creates a service point shape on the canvas.
        /// </summary>
        /// <param name="canvas">The canvas to add the service point to.</param>
        /// <param name="serviceName">The name of the service point.</param>
        /// <param name="left">The left position of the service point.</param>
        /// <param name="top">The top position of the service point.</param>
        /// <param name="width">The width of the service point.</param>
        /// <param name="height">The height of the service point.</param>
        private void CreateServicePoint(Canvas canvas, string serviceName, double left, double top, double width, double height)
        {
            var servicePoint = new Ellipse
            {
                Width = width,
                Height = height,
                Fill = _defaultFill,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Tag = serviceName
            };
            Canvas.SetLeft(servicePoint, left);
            Canvas.SetTop(servicePoint, top);
            canvas.Children.Add(servicePoint);
            _servicePointShapes[serviceName] = servicePoint;

            // Add a label for the service point
            var label = new TextBlock
            {
                Text = serviceName,
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                Width = width
            };
            Canvas.SetLeft(label, left);
            Canvas.SetTop(label, top - 15);
            canvas.Children.Add(label);
        }

        /// <summary>
        /// Highlights a door on the aircraft diagram.
        /// </summary>
        /// <param name="doorName">The name of the door to highlight.</param>
        public void HighlightDoor(string doorName)
        {
            if (_doorShapes.TryGetValue(doorName, out var door))
            {
                door.Fill = _highlightFill;
            }
        }

        /// <summary>
        /// Sets a door as active (open) on the aircraft diagram.
        /// </summary>
        /// <param name="doorName">The name of the door to set as active.</param>
        /// <param name="isActive">Whether the door is active (open).</param>
        public void SetDoorActive(string doorName, bool isActive)
        {
            if (_doorShapes.TryGetValue(doorName, out var door))
            {
                door.Fill = isActive ? _activeFill : _defaultFill;
            }
        }

        /// <summary>
        /// Resets all door highlights on the aircraft diagram.
        /// </summary>
        public void ResetDoorHighlights()
        {
            foreach (var door in _doorShapes.Values)
            {
                door.Fill = _defaultFill;
            }
        }

        /// <summary>
        /// Highlights a service point on the aircraft diagram.
        /// </summary>
        /// <param name="servicePointName">The name of the service point to highlight.</param>
        public void HighlightServicePoint(string servicePointName)
        {
            if (_servicePointShapes.TryGetValue(servicePointName, out var servicePoint))
            {
                servicePoint.Fill = _highlightFill;
            }
        }

        /// <summary>
        /// Sets a service point as active on the aircraft diagram.
        /// </summary>
        /// <param name="servicePointName">The name of the service point to set as active.</param>
        /// <param name="isActive">Whether the service point is active.</param>
        public void SetServicePointActive(string servicePointName, bool isActive)
        {
            if (_servicePointShapes.TryGetValue(servicePointName, out var servicePoint))
            {
                servicePoint.Fill = isActive ? _activeFill : _defaultFill;
            }
        }

        /// <summary>
        /// Resets all service point highlights on the aircraft diagram.
        /// </summary>
        public void ResetServicePointHighlights()
        {
            foreach (var servicePoint in _servicePointShapes.Values)
            {
                servicePoint.Fill = _defaultFill;
            }
        }
    }
}
