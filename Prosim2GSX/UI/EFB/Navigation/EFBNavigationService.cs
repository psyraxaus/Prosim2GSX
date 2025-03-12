using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace Prosim2GSX.UI.EFB.Navigation
{
    /// <summary>
    /// Navigation service for EFB pages.
    /// </summary>
    public class EFBNavigationService
    {
        private readonly Dictionary<string, IEFBPage> _pages = new Dictionary<string, IEFBPage>();
        private readonly Stack<IEFBPage> _navigationHistory = new Stack<IEFBPage>();
        private IEFBPage _currentPage;
        private readonly ContentControl _contentControl;
        
        /// <summary>
        /// Event raised when the current page changes.
        /// </summary>
        public event EventHandler<IEFBPage> CurrentPageChanged;
        
        /// <summary>
        /// Event raised when the navigation history changes.
        /// </summary>
        public event EventHandler<IReadOnlyCollection<IEFBPage>> NavigationHistoryChanged;
        
        /// <summary>
        /// Event raised when navigation is about to occur.
        /// </summary>
        public event EventHandler<NavigationEventArgs> Navigating;
        
        /// <summary>
        /// Event raised when navigation has completed.
        /// </summary>
        public event EventHandler<NavigationEventArgs> Navigated;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EFBNavigationService"/> class.
        /// </summary>
        /// <param name="contentControl">The content control to display pages in.</param>
        public EFBNavigationService(ContentControl contentControl)
        {
            _contentControl = contentControl ?? throw new ArgumentNullException(nameof(contentControl));
        }
        
        /// <summary>
        /// Gets the current page.
        /// </summary>
        public IEFBPage CurrentPage => _currentPage;
        
        /// <summary>
        /// Gets the navigation history.
        /// </summary>
        public IReadOnlyCollection<IEFBPage> NavigationHistory => _navigationHistory.ToList().AsReadOnly();
        
        /// <summary>
        /// Gets a value indicating whether navigation back is possible.
        /// </summary>
        public bool CanGoBack => _navigationHistory.Count > 0;
        
        /// <summary>
        /// Registers a page with the navigation service.
        /// </summary>
        /// <param name="page">The page to register.</param>
        public void RegisterPage(IEFBPage page)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }
            
            _pages[page.Title] = page;
        }
        
        /// <summary>
        /// Registers a page with the navigation service.
        /// </summary>
        /// <param name="key">The key to register the page with.</param>
        /// <param name="pageType">The type of the page to register.</param>
        public void RegisterPage(string key, Type pageType)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }
            
            if (pageType == null)
            {
                throw new ArgumentNullException(nameof(pageType));
            }
            
            if (!typeof(IEFBPage).IsAssignableFrom(pageType))
            {
                throw new ArgumentException($"Type {pageType.Name} does not implement IEFBPage", nameof(pageType));
            }
            
            // Create an instance of the page
            var page = (IEFBPage)Activator.CreateInstance(pageType);
            
            // Register the page
            _pages[key] = page;
        }
        
        /// <summary>
        /// Navigates to a page by title.
        /// </summary>
        /// <param name="pageTitle">The title of the page to navigate to.</param>
        /// <param name="parameter">Optional navigation parameter.</param>
        /// <returns>True if navigation was successful, false otherwise.</returns>
        public bool NavigateTo(string pageTitle, object parameter = null)
        {
            if (string.IsNullOrEmpty(pageTitle))
            {
                throw new ArgumentNullException(nameof(pageTitle));
            }
            
            if (!_pages.TryGetValue(pageTitle, out var page))
            {
                return false;
            }
            
            // Raise the Navigating event
            Navigating?.Invoke(this, new NavigationEventArgs(pageTitle, parameter));
            
            bool result = NavigateTo(page);
            
            if (result)
            {
                // Raise the Navigated event
                Navigated?.Invoke(this, new NavigationEventArgs(pageTitle, parameter));
            }
            
            return result;
        }
        
        /// <summary>
        /// Navigates to a page.
        /// </summary>
        /// <param name="page">The page to navigate to.</param>
        /// <returns>True if navigation was successful, false otherwise.</returns>
        public bool NavigateTo(IEFBPage page)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }
            
            if (!page.CanNavigateTo)
            {
                return false;
            }
            
            if (_currentPage != null)
            {
                _currentPage.OnNavigatedFrom();
                _navigationHistory.Push(_currentPage);
                _currentPage.OnDeactivated();
            }
            
            _currentPage = page;
            _contentControl.Content = page.Content;
            
            _currentPage.OnNavigatedTo();
            _currentPage.OnActivated();
            
            CurrentPageChanged?.Invoke(this, _currentPage);
            NavigationHistoryChanged?.Invoke(this, NavigationHistory);
            
            return true;
        }
        
        /// <summary>
        /// Navigates back to the previous page.
        /// </summary>
        /// <returns>True if navigation was successful, false otherwise.</returns>
        public bool GoBack()
        {
            if (!CanGoBack)
            {
                return false;
            }
            
            if (_currentPage != null)
            {
                _currentPage.OnNavigatedFrom();
                _currentPage.OnDeactivated();
            }
            
            _currentPage = _navigationHistory.Pop();
            _contentControl.Content = _currentPage.Content;
            
            _currentPage.OnNavigatedTo();
            _currentPage.OnActivated();
            
            CurrentPageChanged?.Invoke(this, _currentPage);
            NavigationHistoryChanged?.Invoke(this, NavigationHistory);
            
            return true;
        }
        
        /// <summary>
        /// Clears the navigation history.
        /// </summary>
        public void ClearHistory()
        {
            _navigationHistory.Clear();
            NavigationHistoryChanged?.Invoke(this, NavigationHistory);
        }
        
        /// <summary>
        /// Refreshes the current page.
        /// </summary>
        public void RefreshCurrentPage()
        {
            _currentPage?.OnRefresh();
        }
        
        /// <summary>
        /// Gets all registered pages.
        /// </summary>
        /// <returns>A collection of all registered pages.</returns>
        public IReadOnlyCollection<IEFBPage> GetAllPages()
        {
            return _pages.Values.ToList().AsReadOnly();
        }
        
        /// <summary>
        /// Gets all pages that are visible in the navigation menu.
        /// </summary>
        /// <returns>A collection of pages that are visible in the navigation menu.</returns>
        public IReadOnlyCollection<IEFBPage> GetMenuPages()
        {
            return _pages.Values.Where(p => p.IsVisibleInMenu).ToList().AsReadOnly();
        }
    }
}
