using System;
using Prosim2GSX.Models;
using Prosim2GSX.Services;
using Prosim2GSX.UI.EFB.Navigation;
using Prosim2GSX.UI.EFB.Themes;

namespace Prosim2GSX.UI.EFB.Views.Settings
{
    /// <summary>
    /// Adapter class for SettingsPage that inherits from PageAdapterBase.
    /// </summary>
    public class SettingsPageAdapter : PageAdapterBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsPageAdapter"/> class.
        /// </summary>
        /// <param name="serviceModel">The service model.</param>
        /// <param name="navigationService">The navigation service.</param>
        /// <param name="logger">The logger.</param>
        public SettingsPageAdapter(
            ServiceModel serviceModel,
            EFBNavigationService navigationService = null,
            ILogger logger = null)
            : base(new SettingsPage(
                serviceModel,
                navigationService,
                logger), 
                logger)
        {
            logger?.Log(LogLevel.Debug, "SettingsPageAdapter", "SettingsPageAdapter created successfully");
        }
    }
}
