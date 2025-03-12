using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Prosim2GSX.Models;
using Prosim2GSX.UI.EFB.Navigation;
using Prosim2GSX.UI.EFB.ViewModels;

namespace Prosim2GSX.UI.EFB
{
    /// <summary>
    /// Interaction logic for EFBMainWindow.xaml
    /// </summary>
    public partial class EFBMainWindow : Window
    {
        private readonly EFBNavigationService _navigationService;
        private readonly EFBDataBindingService _dataBindingService;
        private readonly Dictionary<string, Button> _menuButtons = new Dictionary<string, Button>();
        private readonly ServiceModel _serviceModel;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EFBMainWindow"/> class.
        /// </summary>
        /// <param name="serviceModel">The service model.</param>
        public EFBMainWindow(ServiceModel serviceModel)
        {
            InitializeComponent();
            
            _serviceModel = serviceModel ?? throw new ArgumentNullException(nameof(serviceModel));
            _dataBindingService = new EFBDataBindingService(_serviceModel);
            _navigationService = new EFBNavigationService(PageContent);
            
            // Set logo
            LogoImage.Source = new BitmapImage(new Uri("pack://application:,,,/Prosim2GSX;component/logo.ico"));
            
            // Subscribe to navigation events
            _navigationService.CurrentPageChanged += OnCurrentPageChanged;
            
            // Initialize navigation buttons
            UpdateNavigationButtons();
            
            // Load resources
            LoadResources();
            
            // Register pages
            RegisterPages();
            
            // Navigate to home page
            _navigationService.NavigateTo("Home");
        }
        
        private void LoadResources()
        {
            // Load resources from App.xaml or other resource dictionaries
            var resources = new ResourceDictionary
            {
                Source = new Uri("/Prosim2GSX;component/UI/EFB/Styles/EFBStyles.xaml", UriKind.Relative)
            };
            
            Application.Current.Resources.MergedDictionaries.Add(resources);
        }
        
        private void RegisterPages()
        {
            // Register pages with the navigation service
            // This will be implemented in Phase 3
        }
        
        private void OnCurrentPageChanged(object sender, IEFBPage e)
        {
            // Update current page title
            CurrentPageTitle.Text = e.Title;
            
            // Update navigation buttons
            UpdateNavigationButtons();
            
            // Update menu buttons
            UpdateMenuButtons();
        }
        
        private void UpdateNavigationButtons()
        {
            // Update back button
            BackButton.IsEnabled = _navigationService.CanGoBack;
        }
        
        private void UpdateMenuButtons()
        {
            // Clear selection
            foreach (var menuBtn in _menuButtons.Values)
            {
                menuBtn.IsEnabled = true;
                menuBtn.Background = FindResource("EFBSecondaryBrush") as SolidColorBrush;
                menuBtn.Foreground = FindResource("EFBForegroundBrush") as SolidColorBrush;
            }
            
            // Set selection for current page
            if (_navigationService.CurrentPage != null && _menuButtons.TryGetValue(_navigationService.CurrentPage.Title, out var selectedBtn))
            {
                selectedBtn.IsEnabled = false;
                selectedBtn.Background = FindResource("EFBPrimaryBrush") as SolidColorBrush;
                selectedBtn.Foreground = Brushes.White;
            }
        }
        
        private void CreateMenuButton(IEFBPage page)
        {
            var menuButton = new Button
            {
                Content = page.Title,
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Style = FindResource("EFBMenuButtonStyle") as Style
            };
            
            menuButton.Click += (sender, e) => _navigationService.NavigateTo(page);
            
            NavigationMenu.Children.Add(menuButton);
            _menuButtons[page.Title] = menuButton;
        }
        
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.GoBack();
        }
        
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.NavigateTo("Home");
        }
        
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _navigationService.RefreshCurrentPage();
        }
        
        /// <summary>
        /// Called when the window is closing.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            
            // Clean up
            _navigationService.CurrentPageChanged -= OnCurrentPageChanged;
            _dataBindingService.Cleanup();
        }
    }
}
