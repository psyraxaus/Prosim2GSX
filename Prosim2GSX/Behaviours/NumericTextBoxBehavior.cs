using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Prosim2GSX.Behaviours
{
    /// <summary>
    /// Provides attached behavior for restricting input to numeric values in TextBox controls
    /// </summary>
    public static class NumericTextBoxBehavior
    {
        /// <summary>
        /// Identifies the AllowOnlyNumericInput attached property
        /// </summary>
        public static readonly DependencyProperty AllowOnlyNumericInputProperty =
            DependencyProperty.RegisterAttached(
                "AllowOnlyNumericInput",
                typeof(bool),
                typeof(NumericTextBoxBehavior),
                new PropertyMetadata(false, OnAllowOnlyNumericInputChanged));

        /// <summary>
        /// Gets the AllowOnlyNumericInput attached property value
        /// </summary>
        public static bool GetAllowOnlyNumericInput(DependencyObject obj)
        {
            return (bool)obj.GetValue(AllowOnlyNumericInputProperty);
        }

        /// <summary>
        /// Sets the AllowOnlyNumericInput attached property value
        /// </summary>
        public static void SetAllowOnlyNumericInput(DependencyObject obj, bool value)
        {
            obj.SetValue(AllowOnlyNumericInputProperty, value);
        }

        /// <summary>
        /// Handles changes to the AllowOnlyNumericInput property
        /// </summary>
        private static void OnAllowOnlyNumericInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                // First remove existing handlers to prevent duplicates
                textBox.PreviewTextInput -= TextBox_PreviewTextInput;
                textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
                DataObject.RemovePastingHandler(textBox, TextBox_Pasting);

                // Only add new handlers if enabled
                if ((bool)e.NewValue)
                {
                    textBox.PreviewTextInput += TextBox_PreviewTextInput;
                    textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                    DataObject.AddPastingHandler(textBox, TextBox_Pasting);
                }
            }
        }


        /// <summary>
        /// Handles the PreviewTextInput event of TextBox
        /// </summary>
        private static void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                e.Handled = !IsValidNumericInput(textBox.Text, e.Text, textBox.SelectionStart, textBox.SelectionLength);
            }
        }

        /// <summary>
        /// Handles the PreviewKeyDown event of TextBox
        /// </summary>
        private static void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Allow navigation, selection, and editing keys
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the Pasting event of TextBox
        /// </summary>
        private static void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is TextBox textBox && e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsValidNumericInput(textBox.Text, text, textBox.SelectionStart, textBox.SelectionLength))
                {
                    e.CancelCommand();
                }
            }
        }

        /// <summary>
        /// Determines if the input is a valid numeric format
        /// </summary>
        private static bool IsValidNumericInput(string current, string input, int selectionStart, int selectionLength)
        {
            // Calculate the resulting text after the input
            string resultText = current.Substring(0, selectionStart) + input + current.Substring(selectionStart + selectionLength);

            // Empty string is valid
            if (string.IsNullOrEmpty(resultText))
            {
                return true;
            }

            // Allow negative sign at the beginning
            if (resultText == "-")
            {
                return true;
            }

            // Check if the resulting text can be parsed as a real number in invariant culture
            return double.TryParse(resultText, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }
    }
}
