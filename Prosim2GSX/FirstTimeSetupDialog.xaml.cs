using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using Prosim2GSX.Models;

namespace Prosim2GSX
{
    public partial class FirstTimeSetupDialog : Window
    {
        private ServiceModel _model;
        private bool _idValidated = false;

        public FirstTimeSetupDialog(ServiceModel model)
        {
            InitializeComponent();
            _model = model;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // Open the SimBrief website in the default browser
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }

        private void txtSimbriefID_KeyUp(object sender, KeyEventArgs e)
        {
            // Clear any previous validation message
            txtValidationMessage.Text = string.Empty;
            
            // If Enter key is pressed, validate the ID
            if (e.Key == Key.Enter)
            {
                ValidateSimBriefID();
            }
        }

        private void btnValidate_Click(object sender, RoutedEventArgs e)
        {
            ValidateSimBriefID();
        }

        private void ValidateSimBriefID()
        {
            string id = txtSimbriefID.Text.Trim();
            
            // Check if the ID is empty
            if (string.IsNullOrWhiteSpace(id))
            {
                txtValidationMessage.Text = "Please enter a SimBrief ID.";
                btnContinue.IsEnabled = false;
                _idValidated = false;
                return;
            }
            
            // Check if the ID is "0"
            if (id == "0")
            {
                txtValidationMessage.Text = "The SimBrief ID cannot be 0. Please enter a valid ID.";
                btnContinue.IsEnabled = false;
                _idValidated = false;
                return;
            }
            
            // Check if the ID is a valid number
            if (!int.TryParse(id, out _))
            {
                txtValidationMessage.Text = "The SimBrief ID must be a numeric value.";
                btnContinue.IsEnabled = false;
                _idValidated = false;
                return;
            }
            
            // If we get here, the ID is valid
            txtValidationMessage.Text = "SimBrief ID validated successfully!";
            txtValidationMessage.Foreground = System.Windows.Media.Brushes.Green;
            btnContinue.IsEnabled = true;
            _idValidated = true;
            
            // Save the ID to the model
            _model.SetSetting("pilotID", id);
            _model.SimBriefID = id;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Set dialog result to false and close
            DialogResult = false;
            Close();
        }

        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            // Double-check that the ID is validated
            if (!_idValidated)
            {
                MessageBox.Show(
                    "Please validate your SimBrief ID before continuing.",
                    "Validation Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            
            // Set dialog result to true and close
            DialogResult = true;
            Close();
        }
    }
}
