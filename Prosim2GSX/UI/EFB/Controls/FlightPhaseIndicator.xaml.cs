using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Prosim2GSX.UI.EFB.Controls
{
    /// <summary>
    /// Interaction logic for FlightPhaseIndicator.xaml
    /// </summary>
    public partial class FlightPhaseIndicator : UserControl
    {
        /// <summary>
        /// Enum for flight phases.
        /// </summary>
        public enum FlightPhase
        {
            /// <summary>
            /// Preflight phase.
            /// </summary>
            Preflight = 0,
            
            /// <summary>
            /// Departure phase.
            /// </summary>
            Departure = 1,
            
            /// <summary>
            /// Taxi out phase.
            /// </summary>
            TaxiOut = 2,
            
            /// <summary>
            /// Flight phase.
            /// </summary>
            Flight = 3,
            
            /// <summary>
            /// Taxi in phase.
            /// </summary>
            TaxiIn = 4,
            
            /// <summary>
            /// Arrival phase.
            /// </summary>
            Arrival = 5,
            
            /// <summary>
            /// Turnaround phase.
            /// </summary>
            Turnaround = 6
        }
        
        /// <summary>
        /// Dependency property for CurrentPhase.
        /// </summary>
        public static readonly DependencyProperty CurrentPhaseProperty =
            DependencyProperty.Register("CurrentPhase", typeof(FlightPhase), typeof(FlightPhaseIndicator),
                new PropertyMetadata(FlightPhase.Preflight, OnCurrentPhaseChanged));
        
        /// <summary>
        /// Gets or sets the current flight phase.
        /// </summary>
        public FlightPhase CurrentPhase
        {
            get { return (FlightPhase)GetValue(CurrentPhaseProperty); }
            set { SetValue(CurrentPhaseProperty, value); }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FlightPhaseIndicator"/> class.
        /// </summary>
        public FlightPhaseIndicator()
        {
            InitializeComponent();
            UpdatePhaseIndicator(CurrentPhase);
        }
        
        private static void OnCurrentPhaseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FlightPhaseIndicator)d;
            var newValue = (FlightPhase)e.NewValue;
            
            control.UpdatePhaseIndicator(newValue);
        }
        
        private void UpdatePhaseIndicator(FlightPhase phase)
        {
            // Reset all styles
            ResetStyles();
            
            // Update current phase text
            CurrentPhaseText.Text = GetPhaseText(phase);
            
            // Update styles based on current phase
            switch (phase)
            {
                case FlightPhase.Preflight:
                    Circle1.Style = FindResource("ActivePhaseCircleStyle") as Style;
                    Label1.Style = FindResource("ActivePhaseTextStyle") as Style;
                    break;
                    
                case FlightPhase.Departure:
                    Circle1.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line1.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle2.Style = FindResource("ActivePhaseCircleStyle") as Style;
                    Label2.Style = FindResource("ActivePhaseTextStyle") as Style;
                    break;
                    
                case FlightPhase.TaxiOut:
                    Circle1.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line1.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle2.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line2.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle3.Style = FindResource("ActivePhaseCircleStyle") as Style;
                    Label3.Style = FindResource("ActivePhaseTextStyle") as Style;
                    break;
                    
                case FlightPhase.Flight:
                    Circle1.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line1.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle2.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line2.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle3.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line3.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle4.Style = FindResource("ActivePhaseCircleStyle") as Style;
                    Label4.Style = FindResource("ActivePhaseTextStyle") as Style;
                    break;
                    
                case FlightPhase.TaxiIn:
                    Circle1.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line1.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle2.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line2.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle3.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line3.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle4.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line4.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle5.Style = FindResource("ActivePhaseCircleStyle") as Style;
                    Label5.Style = FindResource("ActivePhaseTextStyle") as Style;
                    break;
                    
                case FlightPhase.Arrival:
                    Circle1.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line1.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle2.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line2.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle3.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line3.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle4.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line4.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle5.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line5.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle6.Style = FindResource("ActivePhaseCircleStyle") as Style;
                    Label6.Style = FindResource("ActivePhaseTextStyle") as Style;
                    break;
                    
                case FlightPhase.Turnaround:
                    Circle1.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line1.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle2.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line2.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle3.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line3.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle4.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line4.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle5.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line5.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle6.Style = FindResource("CompletedPhaseCircleStyle") as Style;
                    Line6.Style = FindResource("CompletedPhaseLineStyle") as Style;
                    Circle7.Style = FindResource("ActivePhaseCircleStyle") as Style;
                    Label7.Style = FindResource("ActivePhaseTextStyle") as Style;
                    break;
            }
        }
        
        private void ResetStyles()
        {
            // Reset circle styles
            Circle1.Style = FindResource("PhaseCircleStyle") as Style;
            Circle2.Style = FindResource("PhaseCircleStyle") as Style;
            Circle3.Style = FindResource("PhaseCircleStyle") as Style;
            Circle4.Style = FindResource("PhaseCircleStyle") as Style;
            Circle5.Style = FindResource("PhaseCircleStyle") as Style;
            Circle6.Style = FindResource("PhaseCircleStyle") as Style;
            Circle7.Style = FindResource("PhaseCircleStyle") as Style;
            
            // Reset line styles
            Line1.Style = FindResource("PhaseLineStyle") as Style;
            Line2.Style = FindResource("PhaseLineStyle") as Style;
            Line3.Style = FindResource("PhaseLineStyle") as Style;
            Line4.Style = FindResource("PhaseLineStyle") as Style;
            Line5.Style = FindResource("PhaseLineStyle") as Style;
            Line6.Style = FindResource("PhaseLineStyle") as Style;
            
            // Reset label styles
            Label1.Style = FindResource("PhaseTextStyle") as Style;
            Label2.Style = FindResource("PhaseTextStyle") as Style;
            Label3.Style = FindResource("PhaseTextStyle") as Style;
            Label4.Style = FindResource("PhaseTextStyle") as Style;
            Label5.Style = FindResource("PhaseTextStyle") as Style;
            Label6.Style = FindResource("PhaseTextStyle") as Style;
            Label7.Style = FindResource("PhaseTextStyle") as Style;
        }
        
        private string GetPhaseText(FlightPhase phase)
        {
            switch (phase)
            {
                case FlightPhase.Preflight:
                    return "PREFLIGHT";
                case FlightPhase.Departure:
                    return "DEPARTURE";
                case FlightPhase.TaxiOut:
                    return "TAXI OUT";
                case FlightPhase.Flight:
                    return "FLIGHT";
                case FlightPhase.TaxiIn:
                    return "TAXI IN";
                case FlightPhase.Arrival:
                    return "ARRIVAL";
                case FlightPhase.Turnaround:
                    return "TURNAROUND";
                default:
                    return "UNKNOWN";
            }
        }
    }
}
