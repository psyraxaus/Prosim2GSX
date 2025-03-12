using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Prosim2GSX.UI.EFB.Navigation
{
    /// <summary>
    /// Navigation event arguments.
    /// </summary>
    public class NavigationEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationEventArgs"/> class.
        /// </summary>
        /// <param name="pageKey">The page key.</param>
        /// <param name="parameter">The navigation parameter.</param>
        public NavigationEventArgs(string pageKey, object parameter)
        {
            PageKey = pageKey;
            Parameter = parameter;
        }

        /// <summary>
        /// Gets the page key.
        /// </summary>
        public string PageKey { get; }

        /// <summary>
        /// Gets the navigation parameter.
        /// </summary>
        public object Parameter { get; }
    }

    /// <summary>
    /// Service for navigating between EFB pages.
    /// </summary>
    public class EFBNavigationService
    {
        private readonly Dictionary<string, Type> _pageTypes = new();
        private readonly Stack<(string PageKey, object Parameter)> _navigationHistory = new();
        private readonly Stack<(string PageKey, object Parameter)> _forwardStack = new();
        private readonly ContentControl _contentControl;
        private IEFBPage _currentPage;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFBNavigationService"/> class.
        /// </summary>
        /// <param name="contentControl">The content control to display pages in.</param>
        public EFBNavigationService(ContentControl contentControl)
        {
            _contentControl = contentControl ?? throw new ArgumentNullException(nameof(contentControl));
        }

        /// <summary>
        /// Event raised when navigating to a page.
        /// </summary>
        public event EventHandler<NavigationEventArgs> Navigating;

        /// <summary>
        /// Event raised when navigated to a page.
        /// </summary>
        public event EventHandler<NavigationEventArgs> Navigated;

        /// <summary>
        /// Gets the current page key.
        /// </summary>
        public string CurrentPageKey { get; private set; }

        /// <summary>
        /// Registers a page type with the navigation service.
        /// </summary>
        /// <param name="key">The page key.</param>
        /// <param name="pageType">The page type.</param>
        public void RegisterPage(string key, Type pageType)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Page key cannot be null or empty.", nameof(key));
            }

            if (pageType == null)
            {
                throw new ArgumentNullException(nameof(pageType));
            }

            if (!typeof(IEFBPage).IsAssignableFrom(pageType))
            {
                throw new ArgumentException($"Page type must implement {nameof(IEFBPage)}.", nameof(pageType));
            }

            _pageTypes[key] = pageType;
        }

        /// <summary>
        /// Navigates to a page.
        /// </summary>
        /// <param name="pageKey">The page key.</param>
        /// <param name="parameter">The navigation parameter.</param>
        /// <returns>True if navigation was successful, false otherwise.</returns>
        public bool NavigateTo(string pageKey, object parameter = null)
        {
            if (string.IsNullOrWhiteSpace(pageKey))
            {
                throw new ArgumentException("Page key cannot be null or empty.", nameof(pageKey));
            }

            if (!_pageTypes.TryGetValue(pageKey, out var pageType))
            {
                throw new ArgumentException($"No page registered with key '{pageKey}'.", nameof(pageKey));
            }

            // Check if the current page allows navigation away
            if (_currentPage != null && !_currentPage.CanNavigateAway())
            {
                return false;
            }

            // Raise the Navigating event
            Navigating?.Invoke(this, new NavigationEventArgs(pageKey, parameter));

            // Create the new page
            var page = (IEFBPage)Activator.CreateInstance(pageType);

            // Call OnNavigatedFrom on the current page
            _currentPage?.OnNavigatedFrom();

            // Update the navigation history
            if (_currentPage != null)
            {
                _navigationHistory.Push((CurrentPageKey, null));
                _forwardStack.Clear();
            }

            // Update the current page
            _currentPage = page;
            CurrentPageKey = pageKey;

            // Set the content control's content
            _contentControl.Content = page;

            // Call OnNavigatedTo on the new page
            _currentPage.OnNavigatedTo(parameter);

            // Raise the Navigated event
            Navigated?.Invoke(this, new NavigationEventArgs(pageKey, parameter));

            return true;
        }

        /// <summary>
        /// Determines whether navigation back is possible.
        /// </summary>
        /// <returns>True if navigation back is possible, false otherwise.</returns>
        public bool CanGoBack()
        {
            return _navigationHistory.Count > 0;
        }

        /// <summary>
        /// Navigates back to the previous page.
        /// </summary>
        /// <returns>True if navigation was successful, false otherwise.</returns>
        public bool GoBack()
        {
            if (!CanGoBack())
            {
                return false;
            }

            // Check if the current page allows navigation away
            if (_currentPage != null && !_currentPage.CanNavigateAway())
            {
                return false;
            }

            // Get the previous page from the history
            var (pageKey, parameter) = _navigationHistory.Pop();

            // Add the current page to the forward stack
            _forwardStack.Push((CurrentPageKey, null));

            // Navigate to the previous page
            return NavigateToWithoutHistory(pageKey, parameter);
        }

        /// <summary>
        /// Determines whether navigation forward is possible.
        /// </summary>
        /// <returns>True if navigation forward is possible, false otherwise.</returns>
        public bool CanGoForward()
        {
            return _forwardStack.Count > 0;
        }

        /// <summary>
        /// Navigates forward to the next page.
        /// </summary>
        /// <returns>True if navigation was successful, false otherwise.</returns>
        public bool GoForward()
        {
            if (!CanGoForward())
            {
                return false;
            }

            // Check if the current page allows navigation away
            if (_currentPage != null && !_currentPage.CanNavigateAway())
            {
                return false;
            }

            // Get the next page from the forward stack
            var (pageKey, parameter) = _forwardStack.Pop();

            // Add the current page to the history
            _navigationHistory.Push((CurrentPageKey, null));

            // Navigate to the next page
            return NavigateToWithoutHistory(pageKey, parameter);
        }

        /// <summary>
        /// Clears the navigation history.
        /// </summary>
        public void ClearHistory()
        {
            _navigationHistory.Clear();
            _forwardStack.Clear();
        }

        private bool NavigateToWithoutHistory(string pageKey, object parameter)
        {
            if (string.IsNullOrWhiteSpace(pageKey))
            {
                throw new ArgumentException("Page key cannot be null or empty.", nameof(pageKey));
            }

            if (!_pageTypes.TryGetValue(pageKey, out var pageType))
            {
                throw new ArgumentException($"No page registered with key '{pageKey}'.", nameof(pageKey));
            }

            // Raise the Navigating event
            Navigating?.Invoke(this, new NavigationEventArgs(pageKey, parameter));

            // Create the new page
            var page = (IEFBPage)Activator.CreateInstance(pageType);

            // Call OnNavigatedFrom on the current page
            _currentPage?.OnNavigatedFrom();

            // Update the current page
            _currentPage = page;
            CurrentPageKey = pageKey;

            // Set the content control's content
            _contentControl.Content = page;

            // Call OnNavigatedTo on the new page
            _currentPage.OnNavigatedTo(parameter);

            // Raise the Navigated event
            Navigated?.Invoke(this, new NavigationEventArgs(pageKey, parameter));

            return true;
        }
    }
}
