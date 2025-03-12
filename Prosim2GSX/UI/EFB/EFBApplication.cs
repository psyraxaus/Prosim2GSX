using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Prosim2GSX.UI.EFB.Navigation;
using Prosim2GSX.Models;
using Prosim2GSX.UI.EFB.Themes;
using Prosim2GSX.UI.EFB.ViewModels;
using Prosim2GSX.UI.EFB.Windows;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB
{
    /// <summary>
    /// Tracks the state of EFB UI initialization for diagnostic purposes
    /// </summary>
    public enum EFBInitializationState
    {
        NotStarted,
        ThemeManagerInitializing,
        ThemeManagerInitialized,
        WindowManagerInitializing,
        WindowManagerInitialized,
        DataBindingInitializing,
        DataBindingInitialized,
        PagesRegistering,
        PagesRegistered,
        Completed,
        Failed
    }

    /// <summary>
    /// Main application class for the EFB UI.
    /// </summary>
    public class EFBApplication
    {
        private readonly ServiceModel _serviceModel;
        private readonly ILogger _logger;
        private EFBThemeManager _themeManager;
        private EFBWindowManager _windowManager;
        private EFBDataBindingService _dataBindingService;
        private EFBWindow _mainWindow;
        private bool _isInitialized;
        private EFBInitializationState _initializationState = EFBInitializationState.NotStarted;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFBApplication"/> class.
        /// </summary>
        /// <param name="serviceModel">The service model.</param>
        /// <param name="logger">The logger instance.</param>
        public EFBApplication(ServiceModel serviceModel, ILogger logger = null)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));
            _logger = logger;
            _logger?.Log(LogLevel.Debug, "EFBApplication:Constructor", "EFBApplication instance created");
        }

        /// <summary>
        /// Gets a value indicating whether the application is initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets the current initialization state.
        /// </summary>
        public EFBInitializationState InitializationState => _initializationState;

        /// <summary>
        /// Gets the theme manager.
        /// </summary>
        public EFBThemeManager ThemeManager => _themeManager;

        /// <summary>
        /// Gets the window manager.
        /// </summary>
        public EFBWindowManager WindowManager => _windowManager;

        /// <summary>
        /// Gets the data binding service.
        /// </summary>
        public EFBDataBindingService DataBindingService => _dataBindingService;

        /// <summary>
        /// Gets the main window.
        /// </summary>
        public EFBWindow MainWindow => _mainWindow;

        /// <summary>
        /// Initializes the EFB application.
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized)
            {
                _logger?.Log(LogLevel.Debug, "EFBApplication:InitializeAsync", "Application already initialized, skipping initialization");
                return true;
            }

            try
            {
                _logger?.Log(LogLevel.Debug, "EFBApplication:InitializeAsync", "Starting EFB application initialization");
                
                // Initialize the theme manager
                UpdateInitializationState(EFBInitializationState.ThemeManagerInitializing);
                _logger?.Log(LogLevel.Debug, "EFBApplication:InitializeAsync", "Initializing theme manager");
                _themeManager = new EFBThemeManager(_logger);
                
                // Load themes from the themes directory
                var themesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UI", "EFB", "Assets", "Themes");
                bool themesDirectoryExists = Directory.Exists(themesDirectory);
                _logger?.Log(LogLevel.Debug, "EFBApplication:InitializeAsync", 
                    $"Themes directory exists: {themesDirectoryExists}, Path: {themesDirectory}");
                
                // Ensure the directory exists
                Directory.CreateDirectory(themesDirectory);
                
                // Load themes
                _logger?.Log(LogLevel.Debug, "EFBApplication:InitializeAsync", "Loading themes from directory");
                await _themeManager.LoadThemesAsync(themesDirectory);
                
                // Check if themes were loaded
                int themeCount = _themeManager.Themes?.Count ?? 0;
                _logger?.Log(LogLevel.Debug, "EFBApplication:InitializeAsync", $"Loaded {themeCount} themes");
                
                // Apply default theme if no themes were loaded
                if (themeCount == 0)
                {
                    _logger?.Log(LogLevel.Warning, "EFBApplication:InitializeAsync", 
                        "No themes found, applying default theme");
                    _themeManager.ApplyDefaultTheme();
                }
                
                UpdateInitializationState(EFBInitializationState.ThemeManagerInitialized);

                // Initialize the window manager
                UpdateInitializationState(EFBInitializationState.WindowManagerInitializing);
                _logger?.Log(LogLevel.Debug, "EFBApplication:InitializeAsync", "Initializing window manager");
                _windowManager = new EFBWindowManager(_themeManager, _logger);
                UpdateInitializationState(EFBInitializationState.WindowManagerInitialized);

                // Initialize the data binding service
                UpdateInitializationState(EFBInitializationState.DataBindingInitializing);
                _logger?.Log(LogLevel.Debug, "EFBApplication:InitializeAsync", "Initializing data binding service");
                _dataBindingService = new EFBDataBindingService(_serviceModel, 500, _logger);
                UpdateInitializationState(EFBInitializationState.DataBindingInitialized);

                // Register pages with the window manager
                UpdateInitializationState(EFBInitializationState.PagesRegistering);
                _logger?.Log(LogLevel.Debug, "EFBApplication:InitializeAsync", "Registering pages with window manager");
                RegisterPages();
                UpdateInitializationState(EFBInitializationState.PagesRegistered);

                _isInitialized = true;
                UpdateInitializationState(EFBInitializationState.Completed);
                _logger?.Log(LogLevel.Information, "EFBApplication:InitializeAsync", "EFB application initialization completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                UpdateInitializationState(EFBInitializationState.Failed);
                _logger?.Log(LogLevel.Error, "EFBApplication:InitializeAsync", ex, 
                    $"Failed to initialize EFB application at state: {_initializationState}");
                
                // Log additional diagnostic information
                LogDiagnosticInformation();
                
                return false;
            }
        }

        /// <summary>
        /// Starts the EFB application.
        /// </summary>
        /// <returns>True if the application was started successfully, false otherwise.</returns>
        public bool Start()
        {
            if (!_isInitialized)
            {
                string errorMessage = "EFB application must be initialized before starting.";
                _logger?.Log(LogLevel.Error, "EFBApplication:Start", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            try
            {
                _logger?.Log(LogLevel.Debug, "EFBApplication:Start", "Starting EFB application");
                
                // Create the main window
                _logger?.Log(LogLevel.Debug, "EFBApplication:Start", "Creating main window");
                _mainWindow = _windowManager.CreateWindow();
                
                // Show the main window
                _logger?.Log(LogLevel.Debug, "EFBApplication:Start", "Showing main window");
                _mainWindow.Show();
                
                // Navigate to the home page
                _logger?.Log(LogLevel.Debug, "EFBApplication:Start", "Navigating to home page");
                _mainWindow.NavigateTo("Home");

                _logger?.Log(LogLevel.Information, "EFBApplication:Start", "EFB application started successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "EFBApplication:Start", ex, "Failed to start EFB application");
                
                // Log additional diagnostic information
                LogDiagnosticInformation();
                
                return false;
            }
        }

        /// <summary>
        /// Stops the EFB application.
        /// </summary>
        public void Stop()
        {
            try
            {
                _logger?.Log(LogLevel.Debug, "EFBApplication:Stop", "Stopping EFB application");
                
                // Close all windows
                _logger?.Log(LogLevel.Debug, "EFBApplication:Stop", "Closing all windows");
                _windowManager?.CloseAllWindows();
                
                // Clean up the data binding service
                _logger?.Log(LogLevel.Debug, "EFBApplication:Stop", "Cleaning up data binding service");
                _dataBindingService?.Cleanup();
                
                _isInitialized = false;
                _logger?.Log(LogLevel.Information, "EFBApplication:Stop", "EFB application stopped successfully");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "EFBApplication:Stop", ex, "Error stopping EFB application");
            }
        }

        /// <summary>
        /// Updates the initialization state and logs the change.
        /// </summary>
        /// <param name="newState">The new initialization state.</param>
        private void UpdateInitializationState(EFBInitializationState newState)
        {
            _initializationState = newState;
            _logger?.Log(LogLevel.Debug, "EFBApplication:Initialize", $"Initialization state changed to: {newState}");
        }
        
        /// <summary>
        /// Logs diagnostic information about the EFB application state.
        /// </summary>
        private void LogDiagnosticInformation()
        {
            try
            {
                _logger?.Log(LogLevel.Debug, "EFBApplication:Diagnostics", 
                    $"Initialization state: {_initializationState}");
                
                // Theme manager diagnostics
                if (_themeManager != null)
                {
                    _logger?.Log(LogLevel.Debug, "EFBApplication:Diagnostics", 
                        $"Theme manager initialized: {_themeManager != null}, " +
                        $"Theme count: {_themeManager.Themes?.Count ?? 0}");
                }
                else
                {
                    _logger?.Log(LogLevel.Debug, "EFBApplication:Diagnostics", 
                        "Theme manager not initialized");
                }
                
                // Window manager diagnostics
                _logger?.Log(LogLevel.Debug, "EFBApplication:Diagnostics", 
                    $"Window manager initialized: {_windowManager != null}");
                
                // Data binding service diagnostics
                _logger?.Log(LogLevel.Debug, "EFBApplication:Diagnostics", 
                    $"Data binding service initialized: {_dataBindingService != null}");
                
                // Main window diagnostics
                _logger?.Log(LogLevel.Debug, "EFBApplication:Diagnostics", 
                    $"Main window created: {_mainWindow != null}");
                
                // Directory diagnostics
                var themesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UI", "EFB", "Assets", "Themes");
                bool themesDirectoryExists = Directory.Exists(themesDirectory);
                _logger?.Log(LogLevel.Debug, "EFBApplication:Diagnostics", 
                    $"Themes directory exists: {themesDirectoryExists}, Path: {themesDirectory}");
                
                if (themesDirectoryExists)
                {
                    try
                    {
                        var themeFiles = Directory.GetFiles(themesDirectory, "*.json");
                        _logger?.Log(LogLevel.Debug, "EFBApplication:Diagnostics", 
                            $"Theme files found: {themeFiles.Length}");
                        
                        foreach (var file in themeFiles)
                        {
                            _logger?.Log(LogLevel.Debug, "EFBApplication:Diagnostics", 
                                $"Theme file: {Path.GetFileName(file)}, Size: {new FileInfo(file).Length} bytes");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.Log(LogLevel.Debug, "EFBApplication:Diagnostics", 
                            $"Error enumerating theme files: {ex.Message}");
                    }
                }
                
                // System diagnostics
                _logger?.Log(LogLevel.Debug, "EFBApplication:Diagnostics", 
                    $"Available memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Debug, "EFBApplication:Diagnostics", 
                    $"Error collecting diagnostic information: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers pages with the window manager.
        /// </summary>
        private void RegisterPages()
        {
            // TODO: Register actual page implementations
            // For now, we'll just register placeholder pages
            
            // Home page
            _windowManager.RegisterPage(
                "Home",
                typeof(DummyPage), // Replace with actual page type
                "Home",
                "\uE80F"); // Home icon
            
            // Aircraft page
            _windowManager.RegisterPage(
                "Aircraft",
                typeof(Views.Aircraft.AircraftPageAdapter), // Use the adapter instead of the page directly
                "Aircraft",
                "\uE709"); // Aircraft icon
            
            // Services page
            _windowManager.RegisterPage(
                "Services",
                typeof(DummyPage), // Replace with actual page type
                "Services",
                "\uE8F1"); // Services icon
            
            // Plan page
            _windowManager.RegisterPage(
                "Plan",
                typeof(DummyPage), // Replace with actual page type
                "Plan",
                "\uE8A5"); // Plan icon
            
            // Ground page
            _windowManager.RegisterPage(
                "Ground",
                typeof(DummyPage), // Replace with actual page type
                "Ground",
                "\uE945"); // Ground icon
            
            // Audio page
            _windowManager.RegisterPage(
                "Audio",
                typeof(DummyPage), // Replace with actual page type
                "Audio",
                "\uE767"); // Audio icon
            
            // Logs page
            _windowManager.RegisterPage(
                "Logs",
                typeof(DummyPage), // Replace with actual page type
                "Logs",
                "\uE9D9"); // Logs icon
        }
    }

    /// <summary>
    /// Dummy page for placeholder purposes.
    /// </summary>
    public class DummyPage : UserControl, IEFBPage
    {
        /// <summary>
        /// Gets the title of the page.
        /// </summary>
        public string Title => "Dummy Page";

        /// <summary>
        /// Gets the icon of the page.
        /// </summary>
        public string Icon => "\uE8A5";

        /// <summary>
        /// Gets the page content.
        /// </summary>
        public UserControl Content => this;

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
            // Do nothing
        }

        /// <summary>
        /// Called when the page is navigated from.
        /// </summary>
        public void OnNavigatedFrom()
        {
            // Do nothing
        }

        /// <summary>
        /// Called when the page is activated.
        /// </summary>
        public void OnActivated()
        {
            // Do nothing
        }

        /// <summary>
        /// Called when the page is deactivated.
        /// </summary>
        public void OnDeactivated()
        {
            // Do nothing
        }

        /// <summary>
        /// Called when the page is refreshed.
        /// </summary>
        public void OnRefresh()
        {
            // Do nothing
        }
    }
}
