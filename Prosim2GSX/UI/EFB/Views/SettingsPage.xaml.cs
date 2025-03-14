using Prosim2GSX.Models;
using Prosim2GSX.Services;
using Prosim2GSX.UI.EFB.Navigation;

namespace Prosim2GSX.UI.EFB.Views
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : SettingsPageBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsPage"/> class.
        /// </summary>
        public SettingsPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsPage"/> class.
        /// </summary>
        /// <param name="serviceModel">The service model.</param>
        /// <param name="navigationService">The navigation service.</param>
        /// <param name="logger">The logger.</param>
        public SettingsPage(ServiceModel serviceModel, EFBNavigationService navigationService, ILogger logger = null)
            : base(serviceModel, navigationService, logger)
        {
            InitializeComponent();
        }
    }
}
