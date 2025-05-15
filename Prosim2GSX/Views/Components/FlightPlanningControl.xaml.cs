using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Prosim2GSX.ViewModels.Components;

namespace Prosim2GSX.Views.Components
{
    /// <summary>
    /// Interaction logic for FlightPlanningControl.xaml
    /// </summary>
    public partial class FlightPlanningControl : UserControl
    {
        /// <summary>
        /// Creates a new instance of FlightPlanningControl
        /// </summary>
        public FlightPlanningControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handle key down events on the SimBrief ID text box
        /// </summary>
        private void SimbriefId_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is FlightPlanningViewModel viewModel)
            {
                viewModel.SaveSimbriefId();

                // Move focus away
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(txtSimbriefId), null);
                Keyboard.ClearFocus();
            }
        }

        /// <summary>
        /// Handle lost focus events on the SimBrief ID text box
        /// </summary>
        private void SimbriefId_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is FlightPlanningViewModel viewModel)
            {
                viewModel.SaveSimbriefId();
            }
        }
    }
}
