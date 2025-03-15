using System;
using System.Windows;
using System.Windows.Controls;
using Prosim2GSX.Models;
using Prosim2GSX.Services;
using Prosim2GSX.UI.EFB.Navigation;
using Prosim2GSX.UI.EFB.Themes;
using Prosim2GSX.UI.EFB.ViewModels;

namespace Prosim2GSX.UI.EFB.Views.Settings
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page, IEFBPageBehavior
    {
        private readonly SettingsViewModel _viewModel;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsPage"/> class.
        /// </summary>
        /// <param name="serviceModel">The service model.</param>
        /// <param name="navigationService">The navigation service.</param>
        /// <param name="logger">The logger.</param>
        public SettingsPage(ServiceModel serviceModel, EFBNavigationService navigationService = null, ILogger logger = null)
        {
            _logger = logger;
            _logger?.Log(LogLevel.Debug, "SettingsPage", "Creating SettingsPage");
            
            try
            {
                // Create the view model
                _viewModel = new SettingsViewModel(serviceModel, navigationService, logger);
                
                // Initialize the component
                InitializeComponent();
                
                // Set the data context
                DataContext = _viewModel;
                
                _logger?.Log(LogLevel.Debug, "SettingsPage", "SettingsPage created successfully");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "SettingsPage", ex, "Error creating SettingsPage");
                
                // Show a simple error message
                var errorTextBlock = new TextBlock
                {
                    Text = "Error loading settings page. Please check the logs for details.",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };
                
                Content = errorTextBlock;
            }
        }

        #region IEFBPageBehavior Implementation

        /// <summary>
        /// Gets the title of the page.
        /// </summary>
        public string Title => "Settings";

        /// <summary>
        /// Gets the icon of the page.
        /// </summary>
        public string Icon => "\uE713";

        /// <summary>
        /// Gets a value indicating whether the page is visible in the navigation menu.
        /// </summary>
        public bool IsVisibleInMenu => true;

        /// <summary>
        /// Gets a value indicating whether the page can be navigated to.
        /// </summary>
        public bool CanNavigateTo => true;

        /// <summary>
        /// Called when the page is navigated to.
        /// </summary>
        public void OnNavigatedTo()
        {
            _logger?.Log(LogLevel.Debug, "SettingsPage", "OnNavigatedTo");
        }

        /// <summary>
        /// Called when the page is navigated from.
        /// </summary>
        public void OnNavigatedFrom()
        {
            _logger?.Log(LogLevel.Debug, "SettingsPage", "OnNavigatedFrom");
        }

        /// <summary>
        /// Called when the page is activated.
        /// </summary>
        public void OnActivated()
        {
            _logger?.Log(LogLevel.Debug, "SettingsPage", "OnActivated");
        }

        /// <summary>
        /// Called when the page is deactivated.
        /// </summary>
        public void OnDeactivated()
        {
            _logger?.Log(LogLevel.Debug, "SettingsPage", "OnDeactivated");
        }

        /// <summary>
        /// Called when the page is refreshed.
        /// </summary>
        public void OnRefresh()
        {
            _logger?.Log(LogLevel.Debug, "SettingsPage", "OnRefresh");
            
            // Reload settings
            _viewModel?.LoadSettings();
        }

        #endregion
    }
}
