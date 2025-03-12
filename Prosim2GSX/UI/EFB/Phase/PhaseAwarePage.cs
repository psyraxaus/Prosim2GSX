using System;
using System.Windows;
using System.Windows.Controls;
using Prosim2GSX.UI.EFB.Controls;
using Prosim2GSX.UI.EFB.Navigation;

namespace Prosim2GSX.UI.EFB.Phase
{
    /// <summary>
    /// Base class for pages that adapt based on the current flight phase.
    /// </summary>
    public abstract class PhaseAwarePage : UserControl, IEFBPage
    {
        private readonly IPhaseContextService _phaseContextService;
        private PhaseContext _currentContext;
        
        /// <summary>
        /// Gets the title of the page.
        /// </summary>
        public abstract string Title { get; }
        
        /// <summary>
        /// Gets the icon of the page.
        /// </summary>
        public abstract string Icon { get; }
        
        /// <summary>
        /// Gets the page content.
        /// </summary>
        public UserControl Content => this;
        
        /// <summary>
        /// Gets a value indicating whether the page is visible in the navigation menu.
        /// </summary>
        public virtual bool IsVisibleInMenu => true;
        
        /// <summary>
        /// Gets a value indicating whether the page can be navigated to.
        /// </summary>
        public virtual bool CanNavigateTo => true;
        
        /// <summary>
        /// Gets the current flight phase.
        /// </summary>
        public FlightPhaseIndicator.FlightPhase CurrentPhase => _phaseContextService.CurrentPhase;
        
        /// <summary>
        /// Gets the current phase context.
        /// </summary>
        public PhaseContext CurrentContext => _currentContext;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PhaseAwarePage"/> class.
        /// </summary>
        /// <param name="phaseContextService">The phase context service.</param>
        protected PhaseAwarePage(IPhaseContextService phaseContextService)
        {
            _phaseContextService = phaseContextService ?? throw new ArgumentNullException(nameof(phaseContextService));
            _currentContext = _phaseContextService.CurrentContext;
            
            // Subscribe to phase context changes
            _phaseContextService.ContextChanged += OnPhaseContextChanged;
            
            // Set initial layout
            Loaded += OnPageLoaded;
        }
        
        /// <summary>
        /// Called when the page is navigated to.
        /// </summary>
        public virtual void OnNavigatedTo()
        {
            // Update the current context
            _currentContext = _phaseContextService.CurrentContext;
            
            // Apply the current context
            ApplyPhaseContext(_currentContext);
        }
        
        /// <summary>
        /// Called when the page is navigated from.
        /// </summary>
        public virtual void OnNavigatedFrom()
        {
            // Clean up any resources
        }
        
        /// <summary>
        /// Called when the page is activated.
        /// </summary>
        public virtual void OnActivated()
        {
            // Update the current context
            _currentContext = _phaseContextService.CurrentContext;
            
            // Apply the current context
            ApplyPhaseContext(_currentContext);
        }
        
        /// <summary>
        /// Called when the page is deactivated.
        /// </summary>
        public virtual void OnDeactivated()
        {
            // Clean up any resources
        }
        
        /// <summary>
        /// Called when the page is refreshed.
        /// </summary>
        public virtual void OnRefresh()
        {
            // Update the current context
            _currentContext = _phaseContextService.CurrentContext;
            
            // Apply the current context
            ApplyPhaseContext(_currentContext);
        }
        
        /// <summary>
        /// Called when the phase context changes.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnPhaseContextChanged(object sender, PhaseContextChangedEventArgs e)
        {
            // Update the current context
            _currentContext = e.NewContext;
            
            // Apply the new context
            ApplyPhaseContext(_currentContext);
        }
        
        /// <summary>
        /// Applies the phase context to the page.
        /// </summary>
        /// <param name="context">The phase context to apply.</param>
        protected virtual void ApplyPhaseContext(PhaseContext context)
        {
            // Apply layout template
            ApplyLayoutTemplate(context.LayoutConfiguration.LayoutTemplate);
            
            // Apply panel configurations
            foreach (var panelConfig in context.LayoutConfiguration.PanelConfigurations)
            {
                ApplyPanelConfiguration(panelConfig.Key, panelConfig.Value);
            }
            
            // Apply control visibility and enabled state
            foreach (var controlVisibility in context.ControlVisibility)
            {
                ApplyControlVisibility(controlVisibility.Key, controlVisibility.Value);
            }
            
            foreach (var controlEnabled in context.ControlEnabled)
            {
                ApplyControlEnabled(controlEnabled.Key, controlEnabled.Value);
            }
            
            // Apply theme overrides
            if (context.ThemeOverrides != null && context.ThemeOverrides.Count > 0)
            {
                ApplyThemeOverrides(context.ThemeOverrides);
            }
            
            // Apply animations
            ApplyAnimations(context.LayoutConfiguration.AnimationConfiguration);
        }
        
        /// <summary>
        /// Applies a layout template to the page.
        /// </summary>
        /// <param name="templateName">The name of the template to apply.</param>
        protected virtual void ApplyLayoutTemplate(string templateName)
        {
            // Find the template in the resources
            if (TryFindResource(templateName) is ControlTemplate template)
            {
                // Apply the template
                var contentControl = FindName("ContentContainer") as ContentControl;
                if (contentControl != null)
                {
                    contentControl.Template = template;
                }
            }
        }
        
        /// <summary>
        /// Applies a panel configuration to the page.
        /// </summary>
        /// <param name="panelName">The name of the panel.</param>
        /// <param name="configuration">The configuration to apply.</param>
        protected virtual void ApplyPanelConfiguration(string panelName, PanelConfiguration configuration)
        {
            // Find the panel in the page
            var panel = FindName(panelName) as FrameworkElement;
            if (panel != null)
            {
                // Apply visibility
                panel.Visibility = configuration.IsVisible ? Visibility.Visible : Visibility.Collapsed;
                
                // Apply size
                switch (configuration.Size)
                {
                    case PanelSize.Small:
                        panel.Width = 200;
                        panel.Height = 200;
                        break;
                    case PanelSize.Normal:
                        panel.Width = 300;
                        panel.Height = 300;
                        break;
                    case PanelSize.Large:
                        panel.Width = 400;
                        panel.Height = 400;
                        break;
                    case PanelSize.Full:
                        panel.Width = double.NaN; // Auto
                        panel.Height = double.NaN; // Auto
                        break;
                }
                
                // Apply content template
                if (!string.IsNullOrEmpty(configuration.ContentTemplate))
                {
                    if (TryFindResource(configuration.ContentTemplate) is DataTemplate contentTemplate)
                    {
                        if (panel is ContentControl contentControl)
                        {
                            contentControl.ContentTemplate = contentTemplate;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Applies control visibility to the page.
        /// </summary>
        /// <param name="controlName">The name of the control.</param>
        /// <param name="isVisible">Whether the control is visible.</param>
        protected virtual void ApplyControlVisibility(string controlName, bool isVisible)
        {
            // Find the control in the page
            var control = FindName(controlName) as UIElement;
            if (control != null)
            {
                control.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        
        /// <summary>
        /// Applies control enabled state to the page.
        /// </summary>
        /// <param name="controlName">The name of the control.</param>
        /// <param name="isEnabled">Whether the control is enabled.</param>
        protected virtual void ApplyControlEnabled(string controlName, bool isEnabled)
        {
            // Find the control in the page
            var control = FindName(controlName) as UIElement;
            if (control != null)
            {
                control.IsEnabled = isEnabled;
            }
        }
        
        /// <summary>
        /// Applies theme overrides to the page.
        /// </summary>
        /// <param name="themeOverrides">The theme overrides to apply.</param>
        protected virtual void ApplyThemeOverrides(ResourceDictionary themeOverrides)
        {
            // Apply theme overrides to the page resources
            foreach (var key in themeOverrides.Keys)
            {
                Resources[key] = themeOverrides[key];
            }
        }
        
        /// <summary>
        /// Applies animations to the page.
        /// </summary>
        /// <param name="animationConfiguration">The animation configuration to apply.</param>
        protected virtual void ApplyAnimations(AnimationConfiguration animationConfiguration)
        {
            // Apply animations based on the configuration
            // This is a placeholder for actual animation implementation
        }
        
        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            // Apply the current context
            ApplyPhaseContext(_currentContext);
        }
    }
}
