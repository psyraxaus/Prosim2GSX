using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Prosim2GSX.ViewModels.Components;

namespace Prosim2GSX.Views.Components
{
    /// <summary>
    /// Interaction logic for AppSettingsControl.xaml
    /// </summary>
    public partial class AppSettingsControl : UserControl
    {
        /// <summary>
        /// Creates a new instance of AppSettingsControl
        /// </summary>
        public AppSettingsControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handle key down event for custom verbosity text box
        /// </summary>
        private void CustomVerbosity_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is AppSettingsViewModel viewModel)
            {
                viewModel.ApplyCustomVerbosity();
            }
        }

        /// <summary>
        /// Handle lost focus event for custom verbosity text box
        /// </summary>
        private void CustomVerbosity_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is AppSettingsViewModel viewModel)
            {
                viewModel.ApplyCustomVerbosity();
            }
        }
    }
}
