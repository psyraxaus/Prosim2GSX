using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Prosim2GSX.UI.EFB.Navigation;
using Prosim2GSX.UI.EFB.Themes;

namespace Prosim2GSX.UI.EFB.Windows
{
    /// <summary>
    /// Window mode for the EFB window.
    /// </summary>
    public enum EFBWindowMode
    {
        /// <summary>
        /// Normal window mode.
        /// </summary>
        Normal,

        /// <summary>
        /// Compact window mode.
        /// </summary>
        Compact,

        /// <summary>
        /// Full screen window mode.
        /// </summary>
        FullScreen
    }

    /// <summary>
    /// Interaction logic for EFBWindow.xaml
    /// </summary>
    public partial class EFBWindow : Window
    {
        private readonly Dictionary<string, Button> _navigationButtons = new();
        private readonly DispatcherTimer _timer = new();
        private EFBNavigationService _navigationService;
        private EFBThemeManager _themeManager;
        private bool _isDetached;
        private EFBWindowMode _windowMode = EFBWindowMode.Normal;
        private WindowState _previousWindowState;
        private WindowStyle _previousWindowStyle;
        private ResizeMode _previousResizeMode;
        private Rect _previousBounds;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFBWindow"/> class.
        /// </summary>
        public EFBWindow()
        {
            InitializeComponent();

            // Initialize the navigation service
            _navigationService = new EFBNavigationService(PageContent);
            _navigationService.Navigating += NavigationService_Navigating;
            _navigationService.Navigated += NavigationService_Navigated;

            // Initialize the timer for the status bar
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        /// <summary>
        /// Gets a value indicating whether this window is detached.
        /// </summary>
        public bool IsDetached
        {
            get => _isDetached;
            private set
            {
                _isDetached = value;
                DetachButton.Content = value ? "\uE8A9" : "\uE8A7"; // Change icon based on state
                DetachButton.ToolTip = value ? "Attach to main window" : "Detach to secondary monitor";
            }
        }

        /// <summary>
        /// Gets or sets the window mode.
        /// </summary>
        public EFBWindowMode WindowMode
        {
            get => _windowMode;
            set => SetWindowMode(value);
        }

        /// <summary>
        /// Gets the navigation service.
        /// </summary>
        public EFBNavigationService NavigationService => _navigationService;

        /// <summary>
        /// Gets the theme manager.
        /// </summary>
        public EFBThemeManager ThemeManager => _themeManager;

        /// <summary>
        /// Sets the theme manager.
        /// </summary>
        /// <param name="themeManager">The theme manager.</param>
        public void SetThemeManager(EFBThemeManager themeManager)
        {
            _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
            _themeManager.ThemeChanged += ThemeManager_ThemeChanged;
        }

        /// <summary>
        /// Registers a page with the navigation service.
        /// </summary>
        /// <param name="key">The page key.</param>
        /// <param name="pageType">The page type.</param>
        /// <param name="title">The page title.</param>
        /// <param name="icon">The page icon.</param>
        public void RegisterPage(string key, Type pageType, string title, string icon)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Page key cannot be null or empty.", nameof(key));
            }

            if (pageType == null)
            {
                throw new ArgumentNullException(nameof(pageType));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Page title cannot be null or empty.", nameof(title));
            }

            if (string.IsNullOrWhiteSpace(icon))
            {
                throw new ArgumentException("Page icon cannot be null or empty.", nameof(icon));
            }

            // Register the page with the navigation service
            _navigationService.RegisterPage(key, pageType);

            // Create a navigation button for the page
            var button = new Button
            {
                Content = icon,
                ToolTip = title,
                Width = 60,
                Height = 60,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 24,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                Tag = key
            };

            button.Click += NavigationButton_Click;
            _navigationButtons[key] = button;
            NavigationPanel.Children.Add(button);
        }

        /// <summary>
        /// Navigates to a page.
        /// </summary>
        /// <param name="pageKey">The page key.</param>
        /// <param name="parameter">The navigation parameter.</param>
        /// <returns>True if navigation was successful, false otherwise.</returns>
        public bool NavigateTo(string pageKey, object parameter = null)
        {
            return _navigationService.NavigateTo(pageKey, parameter);
        }

        /// <summary>
        /// Sets the status text.
        /// </summary>
        /// <param name="text">The status text.</param>
        public void SetStatus(string text)
        {
            StatusText.Text = text;
        }

        /// <summary>
        /// Detaches the window to a secondary monitor.
        /// </summary>
        public void DetachToSecondaryMonitor()
        {
            if (IsDetached)
            {
                // Attach to main window
                IsDetached = false;
                // TODO: Implement attaching to main window
            }
            else
            {
                // Detach to secondary monitor
                IsDetached = true;
                // TODO: Implement detaching to secondary monitor
            }
        }

        /// <summary>
        /// Sets the window mode.
        /// </summary>
        /// <param name="mode">The window mode.</param>
        public void SetWindowMode(EFBWindowMode mode)
        {
            if (_windowMode == mode)
            {
                return;
            }

            switch (mode)
            {
                case EFBWindowMode.Normal:
                    // Restore the window to normal mode
                    if (_windowMode == EFBWindowMode.FullScreen)
                    {
                        // Restore from full screen
                        WindowStyle = _previousWindowStyle;
                        ResizeMode = _previousResizeMode;
                        WindowState = _previousWindowState;
                        
                        if (_previousBounds.Width > 0 && _previousBounds.Height > 0)
                        {
                            Left = _previousBounds.Left;
                            Top = _previousBounds.Top;
                            Width = _previousBounds.Width;
                            Height = _previousBounds.Height;
                        }
                    }
                    else if (_windowMode == EFBWindowMode.Compact)
                    {
                        // Restore from compact mode
                        Width = 1024;
                        Height = 768;
                        NavigationPanel.Visibility = Visibility.Visible;
                    }
                    break;

                case EFBWindowMode.Compact:
                    // Switch to compact mode
                    if (_windowMode == EFBWindowMode.FullScreen)
                    {
                        // First restore from full screen
                        WindowStyle = _previousWindowStyle;
                        ResizeMode = _previousResizeMode;
                        WindowState = _previousWindowState;
                    }

                    // Then apply compact mode settings
                    Width = 800;
                    Height = 600;
                    NavigationPanel.Visibility = Visibility.Collapsed;
                    break;

                case EFBWindowMode.FullScreen:
                    // Save current window state
                    _previousWindowState = WindowState;
                    _previousWindowStyle = WindowStyle;
                    _previousResizeMode = ResizeMode;
                    _previousBounds = new Rect(Left, Top, Width, Height);

                    // Switch to full screen mode
                    WindowStyle = WindowStyle.None;
                    ResizeMode = ResizeMode.NoResize;
                    WindowState = WindowState.Maximized;
                    break;
            }

            _windowMode = mode;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Apply the default theme if a theme manager is set
            _themeManager?.ApplyDefaultTheme();

            // Update the time display
            UpdateTimeDisplay();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Clean up resources
            _timer.Stop();
            _navigationService.Navigating -= NavigationService_Navigating;
            _navigationService.Navigated -= NavigationService_Navigated;

            if (_themeManager != null)
            {
                _themeManager.ThemeChanged -= ThemeManager_ThemeChanged;
            }

            // Remove event handlers from navigation buttons
            foreach (var button in _navigationButtons.Values)
            {
                button.Click -= NavigationButton_Click;
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Allow dragging the window by the title bar
            if (e.ClickCount == 1)
            {
                DragMove();
            }
            else if (e.ClickCount == 2)
            {
                // Toggle between normal and maximized on double-click
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
        }

        private void DetachButton_Click(object sender, RoutedEventArgs e)
        {
            DetachToSecondaryMonitor();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string pageKey)
            {
                NavigateTo(pageKey);
            }
        }

        private void NavigationService_Navigating(object sender, NavigationEventArgs e)
        {
            // Update the UI to reflect the navigation
            SetStatus($"Navigating to {e.PageKey}...");
        }

        private void NavigationService_Navigated(object sender, NavigationEventArgs e)
        {
            // Update the UI to reflect the navigation
            SetStatus($"Navigated to {e.PageKey}");

            // Update the selected navigation button
            foreach (var button in _navigationButtons.Values)
            {
                button.Background = Brushes.Transparent;
            }

            if (_navigationButtons.TryGetValue(e.PageKey, out var selectedButton))
            {
                selectedButton.Background = (Brush)FindResource("TabSelectedColor");
            }
        }

        private void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            // Update the UI to reflect the theme change
            SetStatus($"Theme changed to {e.NewTheme.Name}");

            // Update the logo image if available
            if (!string.IsNullOrWhiteSpace(e.NewTheme.LogoPath))
            {
                try
                {
                    LogoImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(e.NewTheme.LogoPath));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading logo image: {ex.Message}");
                }
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateTimeDisplay();
        }

        private void UpdateTimeDisplay()
        {
            TimeText.Text = DateTime.Now.ToString("HH:mm:ss");
        }
    }
}
