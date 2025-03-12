using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Prosim2GSX.UI.EFB.Controls.Aircraft
{
    /// <summary>
    /// Interaction logic for AircraftDiagram.xaml
    /// </summary>
    public partial class AircraftDiagram : UserControl
    {
        private const double ZoomIncrement = 0.1;
        private const double MinZoom = 0.5;
        private const double MaxZoom = 2.0;
        private const double DefaultZoom = 1.0;

        public AircraftDiagram()
        {
            InitializeComponent();
            
            // Add mouse wheel event handler for zooming
            this.PreviewMouseWheel += AircraftDiagram_PreviewMouseWheel;
        }

        #region Event Handlers

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomIn();
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomOut();
        }

        private void ResetViewButton_Click(object sender, RoutedEventArgs e)
        {
            ResetZoom();
        }

        private void AircraftDiagram_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                {
                    ZoomIn();
                }
                else
                {
                    ZoomOut();
                }

                e.Handled = true;
            }
        }

        #endregion

        #region Zoom Methods

        /// <summary>
        /// Zooms in on the aircraft diagram
        /// </summary>
        public void ZoomIn()
        {
            double newZoom = DiagramScale.ScaleX + ZoomIncrement;
            if (newZoom <= MaxZoom)
            {
                DiagramScale.ScaleX = newZoom;
                DiagramScale.ScaleY = newZoom;
            }
        }

        /// <summary>
        /// Zooms out on the aircraft diagram
        /// </summary>
        public void ZoomOut()
        {
            double newZoom = DiagramScale.ScaleX - ZoomIncrement;
            if (newZoom >= MinZoom)
            {
                DiagramScale.ScaleX = newZoom;
                DiagramScale.ScaleY = newZoom;
            }
        }

        /// <summary>
        /// Resets the zoom to the default level
        /// </summary>
        public void ResetZoom()
        {
            DiagramScale.ScaleX = DefaultZoom;
            DiagramScale.ScaleY = DefaultZoom;
        }

        /// <summary>
        /// Sets the zoom to a specific level
        /// </summary>
        /// <param name="zoomLevel">The zoom level to set</param>
        public void SetZoom(double zoomLevel)
        {
            if (zoomLevel >= MinZoom && zoomLevel <= MaxZoom)
            {
                DiagramScale.ScaleX = zoomLevel;
                DiagramScale.ScaleY = zoomLevel;
            }
        }

        #endregion

        #region Animation Methods

        /// <summary>
        /// Highlights a specific door on the aircraft diagram
        /// </summary>
        /// <param name="doorType">The type of door to highlight</param>
        public void HighlightDoor(string doorType)
        {
            // Reset all door highlights
            ResetDoorHighlights();

            // Highlight the specified door
            switch (doorType)
            {
                case "ForwardLeft":
                    ForwardLeftDoor.IsHighlighted = true;
                    break;
                case "ForwardRight":
                    ForwardRightDoor.IsHighlighted = true;
                    break;
                case "AftLeft":
                    AftLeftDoor.IsHighlighted = true;
                    break;
                case "AftRight":
                    AftRightDoor.IsHighlighted = true;
                    break;
                case "ForwardCargo":
                    ForwardCargoDoor.IsHighlighted = true;
                    break;
                case "AftCargo":
                    AftCargoDoor.IsHighlighted = true;
                    break;
            }
        }

        /// <summary>
        /// Resets all door highlights
        /// </summary>
        public void ResetDoorHighlights()
        {
            ForwardLeftDoor.IsHighlighted = false;
            ForwardRightDoor.IsHighlighted = false;
            AftLeftDoor.IsHighlighted = false;
            AftRightDoor.IsHighlighted = false;
            ForwardCargoDoor.IsHighlighted = false;
            AftCargoDoor.IsHighlighted = false;
        }

        /// <summary>
        /// Highlights a specific service point on the aircraft diagram
        /// </summary>
        /// <param name="serviceType">The type of service point to highlight</param>
        public void HighlightServicePoint(string serviceType)
        {
            // Reset all service point highlights
            ResetServicePointHighlights();

            // Highlight the specified service point
            switch (serviceType)
            {
                case "Refueling":
                    FuelServicePoint.IsHighlighted = true;
                    break;
                case "Water":
                    WaterServicePoint.IsHighlighted = true;
                    break;
                case "Lavatory":
                    LavatoryServicePoint.IsHighlighted = true;
                    break;
                case "Catering":
                    CateringServicePoint.IsHighlighted = true;
                    break;
            }
        }

        /// <summary>
        /// Resets all service point highlights
        /// </summary>
        public void ResetServicePointHighlights()
        {
            FuelServicePoint.IsHighlighted = false;
            WaterServicePoint.IsHighlighted = false;
            LavatoryServicePoint.IsHighlighted = false;
            CateringServicePoint.IsHighlighted = false;
        }

        #endregion
    }
}
