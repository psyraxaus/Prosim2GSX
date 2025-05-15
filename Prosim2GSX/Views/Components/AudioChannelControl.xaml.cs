using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Prosim2GSX.Views.Components
{
    /// <summary>
    /// Interaction logic for AudioChannelControl.xaml
    /// </summary>
    public partial class AudioChannelControl : UserControl
    {
        /// <summary>
        /// Creates a new instance of AudioChannelControl
        /// </summary>
        public AudioChannelControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles key press events in the process name text box
        /// </summary>
        private void ProcessNameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                // Force update to the binding
                TextBox textBox = sender as TextBox;
                if (textBox != null)
                {
                    BindingExpression binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    binding?.UpdateSource();
                }
            }
        }

        /// <summary>
        /// Handles lost focus events in the process name text box
        /// </summary>
        private void ProcessNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Force update to the binding
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                BindingExpression binding = textBox.GetBindingExpression(TextBox.TextProperty);
                binding?.UpdateSource();
            }
        }

    }
}
