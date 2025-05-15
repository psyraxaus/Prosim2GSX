using Prosim2GSX.Models;
using Prosim2GSX.ViewModels;
using System.Windows;

namespace Prosim2GSX
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The notify icon view model for managing tray icon behavior
        /// </summary>
        protected NotifyIconViewModel notifyModel;

        /// <summary>
        /// The service model that maintains application state
        /// </summary>
        protected ServiceModel serviceModel;

        /// <summary>
        /// The main view model that coordinates all component view models
        /// </summary>
        private MainViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the MainWindow class
        /// </summary>
        /// <param name="notifyModel">The notify icon view model</param>
        /// <param name="serviceModel">The service model for application state</param>
        public MainWindow(NotifyIconViewModel notifyModel, ServiceModel serviceModel)
        {
            InitializeComponent();

            this.notifyModel = notifyModel;
            this.serviceModel = serviceModel;

            // Create the ViewModel and register it with the locator
            _viewModel = new MainViewModel(serviceModel, notifyModel);
            ViewModelLocator.Instance.RegisterViewModel(_viewModel);

            // Set the DataContext for data binding
            DataContext = _viewModel;
        }

        /// <summary>
        /// Handles the window closing event by hiding instead of actually closing
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The event arguments</param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();

            // Clean up ViewModel resources
            _viewModel.Cleanup();
        }

        /// <summary>
        /// Handles the window visibility changed event
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">The event arguments</param>
        protected void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible)
            {
                notifyModel.CanExecuteHideWindow = false;
                notifyModel.CanExecuteShowWindow = true;

                // Notify ViewModel that window is hidden
                _viewModel.OnWindowHidden();
            }
            else
            {
                // Notify ViewModel that window is visible
                _viewModel.OnWindowVisible();
            }
        }
    }
}
