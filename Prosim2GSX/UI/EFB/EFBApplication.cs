using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Prosim2GSX.Models;
using Prosim2GSX.UI.EFB.Themes;
using Prosim2GSX.UI.EFB.ViewModels;
using Prosim2GSX.UI.EFB.Windows;

namespace Prosim2GSX.UI.EFB
{
    /// <summary>
    /// Main application class for the EFB UI.
    /// </summary>
    public class EFBApplication
    {
        private readonly ServiceModel _serviceModel;
        private EFBThemeManager _themeManager;
        private EFBWindowManager _windowManager;
        private EFBDataBindingService _dataBindingService;
        private EFBWindow _mainWindow;
        private bool _isInitialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFBApplication"/> class.
        /// </summary>
        /// <param name="serviceModel">The service model.</param>
        public EFBApplication(ServiceModel serviceModel)
        {
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));
        }

        /// <summary>
        /// Gets a value indicating whether the application is initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

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
                return true;
            }

            try
            {
                // Initialize the theme manager
                _themeManager = new EFBThemeManager();
                
                // Load themes from the themes directory
                var themesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UI", "EFB", "Assets", "Themes");
                Directory.CreateDirectory(themesDirectory); // Ensure the directory exists
                await _themeManager.LoadThemesAsync(themesDirectory);

                // Initialize the window manager
                _windowManager = new EFBWindowManager(_themeManager);

                // Initialize the data binding service
                _dataBindingService = new EFBDataBindingService();
                _dataBindingService.Initialize(_serviceModel);

                // Register pages with the window manager
                RegisterPages();

                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing EFB application: {ex.Message}");
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
                throw new InvalidOperationException("EFB application must be initialized before starting.");
            }

            try
            {
                // Create the main window
                _mainWindow = _windowManager.CreateWindow();
                
                // Show the main window
                _mainWindow.Show();
                
                // Navigate to the home page
                _mainWindow.NavigateTo("Home");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting EFB application: {ex.Message}");
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
                // Close all windows
                _windowManager?.CloseAllWindows();
                
                // Clean up the data binding service
                _dataBindingService?.Cleanup();
                
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping EFB application: {ex.Message}");
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
    public class DummyPage : System.Windows.Controls.UserControl, Navigation.IEFBPage
    {
        /// <summary>
        /// Gets the title of the page.
        /// </summary>
        public string Title => "Dummy Page";

        /// <summary>
        /// Gets the icon source for the page.
        /// </summary>
        public string IconSource => "\uE8A5";

        /// <summary>
        /// Gets a value indicating whether this page can be navigated away from.
        /// </summary>
        /// <returns>True if the page can be navigated away from, false otherwise.</returns>
        public bool CanNavigateAway() => true;

        /// <summary>
        /// Called when the page is navigated to.
        /// </summary>
        /// <param name="parameter">The navigation parameter.</param>
        public void OnNavigatedTo(object parameter)
        {
            // Do nothing
        }

        /// <summary>
        /// Called when the page is navigated away from.
        /// </summary>
        public void OnNavigatedFrom()
        {
            // Do nothing
        }
    }
}
