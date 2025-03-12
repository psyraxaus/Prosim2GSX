using System;
using System.Collections.Generic;
using System.Windows;

namespace Prosim2GSX.UI.EFB.Phase
{
    /// <summary>
    /// Represents the context for a specific flight phase.
    /// </summary>
    public class PhaseContext
    {
        /// <summary>
        /// Gets or sets the title for the phase.
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Gets or sets the description for the phase.
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Gets or sets the list of recommended actions for the phase.
        /// </summary>
        public List<string> RecommendedActions { get; set; } = new List<string>();
        
        /// <summary>
        /// Gets or sets the list of available services for the phase.
        /// </summary>
        public List<string> AvailableServices { get; set; } = new List<string>();
        
        /// <summary>
        /// Gets or sets the dictionary of control visibility settings for the phase.
        /// </summary>
        public Dictionary<string, bool> ControlVisibility { get; set; } = new Dictionary<string, bool>();
        
        /// <summary>
        /// Gets or sets the dictionary of control enabled settings for the phase.
        /// </summary>
        public Dictionary<string, bool> ControlEnabled { get; set; } = new Dictionary<string, bool>();
        
        /// <summary>
        /// Gets or sets the resource dictionary for theme overrides for the phase.
        /// </summary>
        public ResourceDictionary ThemeOverrides { get; set; }
        
        /// <summary>
        /// Gets or sets the list of notifications for the phase.
        /// </summary>
        public List<PhaseNotification> Notifications { get; set; } = new List<PhaseNotification>();
        
        /// <summary>
        /// Gets or sets the list of checklists for the phase.
        /// </summary>
        public List<PhaseChecklist> Checklists { get; set; } = new List<PhaseChecklist>();
        
        /// <summary>
        /// Gets or sets the list of phase actions for the phase.
        /// </summary>
        public List<PhaseAction> Actions { get; set; } = new List<PhaseAction>();
        
        /// <summary>
        /// Gets or sets the layout configuration for the phase.
        /// </summary>
        public PhaseLayoutConfiguration LayoutConfiguration { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PhaseContext"/> class.
        /// </summary>
        public PhaseContext()
        {
            // Initialize with default values
            Title = "Unknown Phase";
            Description = "No description available.";
            ThemeOverrides = new ResourceDictionary();
            LayoutConfiguration = new PhaseLayoutConfiguration();
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PhaseContext"/> class with the specified title and description.
        /// </summary>
        /// <param name="title">The title for the phase.</param>
        /// <param name="description">The description for the phase.</param>
        public PhaseContext(string title, string description)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            ThemeOverrides = new ResourceDictionary();
            LayoutConfiguration = new PhaseLayoutConfiguration();
        }
        
        /// <summary>
        /// Adds a recommended action to the phase context.
        /// </summary>
        /// <param name="action">The action to add.</param>
        public void AddRecommendedAction(string action)
        {
            if (string.IsNullOrEmpty(action))
                throw new ArgumentNullException(nameof(action));
                
            RecommendedActions.Add(action);
        }
        
        /// <summary>
        /// Adds an available service to the phase context.
        /// </summary>
        /// <param name="service">The service to add.</param>
        public void AddAvailableService(string service)
        {
            if (string.IsNullOrEmpty(service))
                throw new ArgumentNullException(nameof(service));
                
            AvailableServices.Add(service);
        }
        
        /// <summary>
        /// Sets the visibility of a control for the phase context.
        /// </summary>
        /// <param name="controlName">The name of the control.</param>
        /// <param name="isVisible">Whether the control is visible.</param>
        public void SetControlVisibility(string controlName, bool isVisible)
        {
            if (string.IsNullOrEmpty(controlName))
                throw new ArgumentNullException(nameof(controlName));
                
            ControlVisibility[controlName] = isVisible;
        }
        
        /// <summary>
        /// Sets the enabled state of a control for the phase context.
        /// </summary>
        /// <param name="controlName">The name of the control.</param>
        /// <param name="isEnabled">Whether the control is enabled.</param>
        public void SetControlEnabled(string controlName, bool isEnabled)
        {
            if (string.IsNullOrEmpty(controlName))
                throw new ArgumentNullException(nameof(controlName));
                
            ControlEnabled[controlName] = isEnabled;
        }
        
        /// <summary>
        /// Adds a theme override to the phase context.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="value">The resource value.</param>
        public void AddThemeOverride(object key, object value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
                
            if (value == null)
                throw new ArgumentNullException(nameof(value));
                
            ThemeOverrides[key] = value;
        }
        
        /// <summary>
        /// Adds a notification to the phase context.
        /// </summary>
        /// <param name="notification">The notification to add.</param>
        public void AddNotification(PhaseNotification notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));
                
            Notifications.Add(notification);
        }
        
        /// <summary>
        /// Adds a checklist to the phase context.
        /// </summary>
        /// <param name="checklist">The checklist to add.</param>
        public void AddChecklist(PhaseChecklist checklist)
        {
            if (checklist == null)
                throw new ArgumentNullException(nameof(checklist));
                
            Checklists.Add(checklist);
        }
        
        /// <summary>
        /// Adds an action to the phase context.
        /// </summary>
        /// <param name="action">The action to add.</param>
        public void AddAction(PhaseAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
                
            Actions.Add(action);
        }
    }
    
    /// <summary>
    /// Represents a notification for a specific flight phase.
    /// </summary>
    public class PhaseNotification
    {
        /// <summary>
        /// Gets or sets the unique identifier for the notification.
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the notification.
        /// </summary>
        public NotificationType Type { get; set; }
        
        /// <summary>
        /// Gets or sets the message for the notification.
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Gets or sets the action to execute when the notification is clicked.
        /// </summary>
        public Action Action { get; set; }
        
        /// <summary>
        /// Gets or sets the trigger condition for the notification.
        /// </summary>
        public Func<bool> TriggerCondition { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the notification should be shown.
        /// </summary>
        public TimeSpan? ShowAfter { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the notification should be dismissed.
        /// </summary>
        public TimeSpan? DismissAfter { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PhaseNotification"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the notification.</param>
        /// <param name="type">The type of the notification.</param>
        /// <param name="message">The message for the notification.</param>
        public PhaseNotification(string id, NotificationType type, string message)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Type = type;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }
    
    /// <summary>
    /// Represents the type of a notification.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// Information notification.
        /// </summary>
        Info,
        
        /// <summary>
        /// Warning notification.
        /// </summary>
        Warning,
        
        /// <summary>
        /// Action notification.
        /// </summary>
        Action,
        
        /// <summary>
        /// Success notification.
        /// </summary>
        Success,
        
        /// <summary>
        /// Error notification.
        /// </summary>
        Error
    }
    
    /// <summary>
    /// Represents a checklist for a specific flight phase.
    /// </summary>
    public class PhaseChecklist
    {
        /// <summary>
        /// Gets or sets the unique identifier for the checklist.
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the title for the checklist.
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Gets or sets the description for the checklist.
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Gets or sets the list of items in the checklist.
        /// </summary>
        public List<PhaseChecklistItem> Items { get; set; } = new List<PhaseChecklistItem>();
        
        /// <summary>
        /// Gets or sets a value indicating whether the checklist is required.
        /// </summary>
        public bool IsRequired { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the checklist is completed.
        /// </summary>
        public bool IsCompleted { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PhaseChecklist"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the checklist.</param>
        /// <param name="title">The title for the checklist.</param>
        public PhaseChecklist(string id, string title)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? throw new ArgumentNullException(nameof(title));
        }
        
        /// <summary>
        /// Adds an item to the checklist.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void AddItem(PhaseChecklistItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
                
            Items.Add(item);
        }
        
        /// <summary>
        /// Checks if all items in the checklist are completed.
        /// </summary>
        /// <returns>True if all items are completed, false otherwise.</returns>
        public bool CheckCompletion()
        {
            IsCompleted = Items.Count > 0 && Items.TrueForAll(item => item.IsCompleted);
            return IsCompleted;
        }
    }
    
    /// <summary>
    /// Represents an item in a checklist.
    /// </summary>
    public class PhaseChecklistItem
    {
        /// <summary>
        /// Gets or sets the unique identifier for the item.
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the text for the item.
        /// </summary>
        public string Text { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the item is completed.
        /// </summary>
        public bool IsCompleted { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the item is required.
        /// </summary>
        public bool IsRequired { get; set; }
        
        /// <summary>
        /// Gets or sets the action to execute when the item is completed.
        /// </summary>
        public Action<bool> CompletionAction { get; set; }
        
        /// <summary>
        /// Gets or sets the function to check if the item is automatically completed.
        /// </summary>
        public Func<bool> AutoCompletionCheck { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PhaseChecklistItem"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the item.</param>
        /// <param name="text">The text for the item.</param>
        public PhaseChecklistItem(string id, string text)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }
        
        /// <summary>
        /// Sets the completion state of the item.
        /// </summary>
        /// <param name="isCompleted">Whether the item is completed.</param>
        public void SetCompleted(bool isCompleted)
        {
            IsCompleted = isCompleted;
            CompletionAction?.Invoke(isCompleted);
        }
        
        /// <summary>
        /// Checks if the item is automatically completed.
        /// </summary>
        /// <returns>True if the item is automatically completed, false otherwise.</returns>
        public bool CheckAutoCompletion()
        {
            if (AutoCompletionCheck != null && AutoCompletionCheck())
            {
                SetCompleted(true);
                return true;
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// Represents an action for a specific flight phase.
    /// </summary>
    public class PhaseAction
    {
        /// <summary>
        /// Gets or sets the unique identifier for the action.
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the name for the action.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the description for the action.
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the action is recommended.
        /// </summary>
        public bool IsRecommended { get; set; }
        
        /// <summary>
        /// Gets or sets the action to execute.
        /// </summary>
        public Action Execute { get; set; }
        
        /// <summary>
        /// Gets or sets the function to check if the action can be executed.
        /// </summary>
        public Func<bool> CanExecute { get; set; }
        
        /// <summary>
        /// Gets or sets the icon for the action.
        /// </summary>
        public string Icon { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PhaseAction"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the action.</param>
        /// <param name="name">The name for the action.</param>
        /// <param name="execute">The action to execute.</param>
        public PhaseAction(string id, string name, Action execute)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }
    }
    
    /// <summary>
    /// Represents the layout configuration for a specific flight phase.
    /// </summary>
    public class PhaseLayoutConfiguration
    {
        /// <summary>
        /// Gets or sets the layout template for the phase.
        /// </summary>
        public string LayoutTemplate { get; set; }
        
        /// <summary>
        /// Gets or sets the panel configuration for the phase.
        /// </summary>
        public Dictionary<string, PanelConfiguration> PanelConfigurations { get; set; } = new Dictionary<string, PanelConfiguration>();
        
        /// <summary>
        /// Gets or sets the animation configuration for the phase.
        /// </summary>
        public AnimationConfiguration AnimationConfiguration { get; set; } = new AnimationConfiguration();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PhaseLayoutConfiguration"/> class.
        /// </summary>
        public PhaseLayoutConfiguration()
        {
            LayoutTemplate = "DefaultLayout";
        }
        
        /// <summary>
        /// Sets the panel configuration for a specific panel.
        /// </summary>
        /// <param name="panelName">The name of the panel.</param>
        /// <param name="configuration">The configuration for the panel.</param>
        public void SetPanelConfiguration(string panelName, PanelConfiguration configuration)
        {
            if (string.IsNullOrEmpty(panelName))
                throw new ArgumentNullException(nameof(panelName));
                
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
                
            PanelConfigurations[panelName] = configuration;
        }
    }
    
    /// <summary>
    /// Represents the configuration for a panel.
    /// </summary>
    public class PanelConfiguration
    {
        /// <summary>
        /// Gets or sets the visibility of the panel.
        /// </summary>
        public bool IsVisible { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the size of the panel.
        /// </summary>
        public PanelSize Size { get; set; } = PanelSize.Normal;
        
        /// <summary>
        /// Gets or sets the position of the panel.
        /// </summary>
        public PanelPosition Position { get; set; } = PanelPosition.Default;
        
        /// <summary>
        /// Gets or sets the content template for the panel.
        /// </summary>
        public string ContentTemplate { get; set; }
    }
    
    /// <summary>
    /// Represents the size of a panel.
    /// </summary>
    public enum PanelSize
    {
        /// <summary>
        /// Small panel size.
        /// </summary>
        Small,
        
        /// <summary>
        /// Normal panel size.
        /// </summary>
        Normal,
        
        /// <summary>
        /// Large panel size.
        /// </summary>
        Large,
        
        /// <summary>
        /// Full panel size.
        /// </summary>
        Full
    }
    
    /// <summary>
    /// Represents the position of a panel.
    /// </summary>
    public enum PanelPosition
    {
        /// <summary>
        /// Default panel position.
        /// </summary>
        Default,
        
        /// <summary>
        /// Top panel position.
        /// </summary>
        Top,
        
        /// <summary>
        /// Bottom panel position.
        /// </summary>
        Bottom,
        
        /// <summary>
        /// Left panel position.
        /// </summary>
        Left,
        
        /// <summary>
        /// Right panel position.
        /// </summary>
        Right,
        
        /// <summary>
        /// Center panel position.
        /// </summary>
        Center
    }
    
    /// <summary>
    /// Represents the configuration for animations.
    /// </summary>
    public class AnimationConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether animations are enabled.
        /// </summary>
        public bool AnimationsEnabled { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the duration of the transition animation.
        /// </summary>
        public TimeSpan TransitionDuration { get; set; } = TimeSpan.FromMilliseconds(300);
        
        /// <summary>
        /// Gets or sets the type of the transition animation.
        /// </summary>
        public TransitionType TransitionType { get; set; } = TransitionType.Fade;
    }
    
    /// <summary>
    /// Represents the type of a transition animation.
    /// </summary>
    public enum TransitionType
    {
        /// <summary>
        /// Fade transition.
        /// </summary>
        Fade,
        
        /// <summary>
        /// Slide transition.
        /// </summary>
        Slide,
        
        /// <summary>
        /// Zoom transition.
        /// </summary>
        Zoom,
        
        /// <summary>
        /// Flip transition.
        /// </summary>
        Flip
    }
}
