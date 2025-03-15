using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Prosim2GSX.UI.EFB.ViewModels.Settings
{
    /// <summary>
    /// View model for a setting.
    /// </summary>
    public class SettingViewModel : ObservableObject
    {
        private string _name;
        /// <summary>
        /// Gets or sets the name of the setting.
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _description;
        /// <summary>
        /// Gets or sets the description of the setting.
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private SettingType _type;
        /// <summary>
        /// Gets or sets the type of the setting.
        /// </summary>
        public SettingType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        private bool _isEnabled = true;
        /// <summary>
        /// Gets or sets a value indicating whether the setting is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        // Toggle properties
        private bool _isToggled;
        /// <summary>
        /// Gets or sets a value indicating whether the toggle is on.
        /// </summary>
        public bool IsToggled
        {
            get => _isToggled;
            set
            {
                if (SetProperty(ref _isToggled, value) && ToggledChanged != null)
                {
                    ToggledChanged(value);
                }
            }
        }
        /// <summary>
        /// Gets or sets the action to call when the toggle is changed.
        /// </summary>
        public Action<bool> ToggledChanged { get; set; }

        // Options properties
        private ObservableCollection<string> _options = new ObservableCollection<string>();
        /// <summary>
        /// Gets or sets the options for the dropdown.
        /// </summary>
        public ObservableCollection<string> Options
        {
            get => _options;
            set => SetProperty(ref _options, value);
        }

        private string _selectedOption;
        /// <summary>
        /// Gets or sets the selected option.
        /// </summary>
        public string SelectedOption
        {
            get => _selectedOption;
            set
            {
                if (SetProperty(ref _selectedOption, value) && SelectedOptionChanged != null)
                {
                    SelectedOptionChanged(value);
                }
            }
        }
        /// <summary>
        /// Gets or sets the action to call when the selected option is changed.
        /// </summary>
        public Action<string> SelectedOptionChanged { get; set; }

        // Text properties
        private string _textValue;
        /// <summary>
        /// Gets or sets the text value.
        /// </summary>
        public string TextValue
        {
            get => _textValue;
            set
            {
                if (SetProperty(ref _textValue, value) && TextChanged != null)
                {
                    TextChanged(value);
                }
            }
        }
        /// <summary>
        /// Gets or sets the action to call when the text is changed.
        /// </summary>
        public Action<string> TextChanged { get; set; }

        // Numeric properties
        private float _numericValue;
        /// <summary>
        /// Gets or sets the numeric value.
        /// </summary>
        public float NumericValue
        {
            get => _numericValue;
            set
            {
                if (SetProperty(ref _numericValue, value) && NumericChanged != null)
                {
                    NumericChanged(value);
                }
            }
        }
        /// <summary>
        /// Gets or sets the action to call when the numeric value is changed.
        /// </summary>
        public Action<float> NumericChanged { get; set; }
    }
}
