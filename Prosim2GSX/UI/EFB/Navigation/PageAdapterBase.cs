using System;
using System.Windows;
using System.Windows.Controls;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.Navigation
{
    /// <summary>
    /// Base class for page adapters that host WPF Page objects.
    /// This class uses a Frame to host a Page, which is required by WPF.
    /// </summary>
    public class PageAdapterBase : UserControl, IEFBPage
    {
        protected Frame _frame;
        protected Page _page;
        protected ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PageAdapterBase"/> class.
        /// </summary>
        /// <param name="page">The page to host.</param>
        /// <param name="logger">The logger instance.</param>
        public PageAdapterBase(Page page, ILogger logger = null)
        {
            _logger = logger;
            _logger?.Log(LogLevel.Debug, "PageAdapterBase", "Creating PageAdapterBase");
            
            try
            {
                // Create a Frame to host the Page
                _frame = new Frame();
                _page = page ?? throw new ArgumentNullException(nameof(page));
                
                // Set the Frame as the content of this UserControl
                Content = _frame;
                
                // Navigate the Frame to the Page
                _frame.Navigate(_page);
                
                _logger?.Log(LogLevel.Debug, "PageAdapterBase", "PageAdapterBase created successfully");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "PageAdapterBase", ex, "Error creating PageAdapterBase");
                
                // Create a fallback UI
                CreateFallbackUI(ex.Message);
            }
        }
        
        /// <summary>
        /// Creates a fallback UI when the page cannot be created.
        /// </summary>
        /// <param name="errorMessage">The error message to display.</param>
        protected void CreateFallbackUI(string errorMessage)
        {
            _logger?.Log(LogLevel.Debug, "PageAdapterBase", "Creating fallback UI");
            
            try
            {
                // Create a simple Grid with a white background
                Grid fallbackGrid = new Grid();
                fallbackGrid.Background = System.Windows.Media.Brushes.White;
                
                // Add a TextBlock with an error message
                TextBlock errorText = new TextBlock();
                errorText.Text = $"Error loading page:\n{errorMessage}\nPlease restart the application.";
                errorText.FontSize = 18;
                errorText.HorizontalAlignment = HorizontalAlignment.Center;
                errorText.VerticalAlignment = VerticalAlignment.Center;
                errorText.TextAlignment = TextAlignment.Center;
                
                fallbackGrid.Children.Add(errorText);
                
                // Set the content of this UserControl to the fallback Grid
                Content = fallbackGrid;
                
                _logger?.Log(LogLevel.Debug, "PageAdapterBase", "Fallback UI created successfully");
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, "PageAdapterBase", ex, "Error creating fallback UI");
            }
        }

        #region IEFBPage Implementation

        /// <summary>
        /// Gets the title of the page.
        /// </summary>
        public string Title => (_page as IEFBPageBehavior)?.Title ?? "Page";
        
        /// <summary>
        /// Gets the icon of the page.
        /// </summary>
        public string Icon => (_page as IEFBPageBehavior)?.Icon ?? "\uE8A5";
        
        /// <summary>
        /// Gets the page content.
        /// </summary>
        UserControl IEFBPage.Content => this;
        
        /// <summary>
        /// Gets a value indicating whether the page is visible in the navigation menu.
        /// </summary>
        public bool IsVisibleInMenu => (_page as IEFBPageBehavior)?.IsVisibleInMenu ?? true;
        
        /// <summary>
        /// Gets a value indicating whether the page can be navigated to.
        /// </summary>
        public bool CanNavigateTo => (_page as IEFBPageBehavior)?.CanNavigateTo ?? true;
        
        /// <summary>
        /// Called when the page is navigated to.
        /// </summary>
        public virtual void OnNavigatedTo()
        {
            (_page as IEFBPageBehavior)?.OnNavigatedTo();
        }
        
        /// <summary>
        /// Called when the page is navigated from.
        /// </summary>
        public virtual void OnNavigatedFrom()
        {
            (_page as IEFBPageBehavior)?.OnNavigatedFrom();
        }
        
        /// <summary>
        /// Called when the page is activated.
        /// </summary>
        public virtual void OnActivated()
        {
            (_page as IEFBPageBehavior)?.OnActivated();
        }
        
        /// <summary>
        /// Called when the page is deactivated.
        /// </summary>
        public virtual void OnDeactivated()
        {
            (_page as IEFBPageBehavior)?.OnDeactivated();
        }
        
        /// <summary>
        /// Called when the page is refreshed.
        /// </summary>
        public virtual void OnRefresh()
        {
            (_page as IEFBPageBehavior)?.OnRefresh();
        }

        #endregion
    }
}
