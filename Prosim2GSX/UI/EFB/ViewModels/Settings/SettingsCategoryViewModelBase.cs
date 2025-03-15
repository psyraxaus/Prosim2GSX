using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Prosim2GSX.Models;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.ViewModels.Settings
{
    /// <summary>
    /// Base class for settings category view models.
    /// </summary>
    public abstract class SettingsCategoryViewModelBase : ObservableObject
    {
        /// <summary>
        /// The service model.
        /// </summary>
        protected readonly ServiceModel ServiceModel;

        /// <summary>
        /// The logger.
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsCategoryViewModelBase"/> class.
        /// </summary>
        /// <param name="serviceModel">The service model.</param>
        /// <param name="logger">The logger.</param>
        protected SettingsCategoryViewModelBase(ServiceModel serviceModel, ILogger logger = null)
        {
            ServiceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));
            Logger = logger;
        }

        /// <summary>
        /// Gets the title of the category.
        /// </summary>
        public abstract string Title { get; }

        /// <summary>
        /// Gets the icon of the category.
        /// </summary>
        public abstract string Icon { get; }

        /// <summary>
        /// Gets the settings in the category.
        /// </summary>
        public ObservableCollection<SettingViewModel> Settings { get; } = new ObservableCollection<SettingViewModel>();

        /// <summary>
        /// Loads the settings from the service model.
        /// </summary>
        public abstract void LoadSettings();

        /// <summary>
        /// Updates the settings in the category.
        /// </summary>
        public abstract void UpdateSettings();

        /// <summary>
        /// Resets the settings to their default values.
        /// </summary>
        public abstract void ResetToDefaults();
    }
}
