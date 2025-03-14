using Prosim2GSX.Models;
using Prosim2GSX.Services;
using Prosim2GSX.UI.EFB.Navigation;
using Prosim2GSX.UI.EFB.ViewModels;
using System;
using System.Windows.Controls;

namespace Prosim2GSX.UI.EFB.Views
{
    /// <summary>
    /// Base class for the settings page.
    /// </summary>
    public class SettingsPageBase : BasePage<SettingsViewModel>
    {
        protected ILogger _logger;

        /// <summary>
        /// Gets the title of the page.
        /// </summary>
        public override string Title => "Settings";

        /// <summary>
        /// Gets a value indicating whether the page is visible in the navigation menu.
        /// </summary>
        public override bool IsVisibleInMenu => true;

        /// <summary>
        /// Gets the icon for the page.
        /// </summary>
        public override string Icon => "\uE713"; // Settings gear icon

        /// <summary>
        /// Gets the order of the page in the navigation menu.
        /// </summary>
        public virtual int Order => 900; // Place near the end of the menu

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsPageBase"/> class.
        /// </summary>
        protected SettingsPageBase() : base(null)
        {
            // This constructor is used by the XAML designer
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsPageBase"/> class.
        /// </summary>
        /// <param name="serviceModel">The service model.</param>
        /// <param name="navigationService">The navigation service.</param>
        /// <param name="logger">The logger.</param>
        public SettingsPageBase(ServiceModel serviceModel, EFBNavigationService navigationService, ILogger logger = null)
            : base(new SettingsViewModel(serviceModel, navigationService, logger))
        {
            _logger = logger;
            
            try
            {
                _logger?.Log(LogLevel.Debug, "SettingsPageBase", "Initializing SettingsPageBase");
                _logger?.Log(LogLevel.Debug, "SettingsPageBase", "SettingsPageBase initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "SettingsPageBase", ex, "Error initializing SettingsPageBase");
            }
        }

        /// <summary>
        /// Called when the page is navigated to.
        /// </summary>
        public override void OnNavigatedTo()
        {
            _logger?.Log(LogLevel.Debug, "SettingsPageBase", "Navigated to SettingsPage");
        }

        /// <summary>
        /// Called when the page is navigated from.
        /// </summary>
        public override void OnNavigatedFrom()
        {
            _logger?.Log(LogLevel.Debug, "SettingsPageBase", "Navigated from SettingsPage");
        }

        /// <summary>
        /// Called when the page is activated.
        /// </summary>
        public override void OnActivated()
        {
            _logger?.Log(LogLevel.Debug, "SettingsPageBase", "SettingsPage activated");
        }

        /// <summary>
        /// Called when the page is deactivated.
        /// </summary>
        public override void OnDeactivated()
        {
            _logger?.Log(LogLevel.Debug, "SettingsPageBase", "SettingsPage deactivated");
        }

        /// <summary>
        /// Called when the page is refreshed.
        /// </summary>
        public override void OnRefresh()
        {
            _logger?.Log(LogLevel.Debug, "SettingsPageBase", "SettingsPage refreshed");
        }
    }
}
