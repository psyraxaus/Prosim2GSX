using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Prosim2GSX.UI.EFB.Navigation;
using Prosim2GSX.UI.EFB.ViewModels;

namespace Prosim2GSX.UI.EFB.Views
{
    /// <summary>
    /// Base class for EFB pages.
    /// </summary>
    /// <typeparam name="TViewModel">The type of the view model.</typeparam>
    [ContentProperty("PageContent")]
    public abstract class BasePage<TViewModel> : UserControl, IEFBPage where TViewModel : BaseViewModel
    {
        private ContentControl _contentControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasePage{TViewModel}"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        protected BasePage(TViewModel viewModel)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = ViewModel;
            
            // Create a content control to host the page content
            _contentControl = new ContentControl();
            base.Content = _contentControl;
        }
        
        /// <summary>
        /// Gets or sets the page content.
        /// </summary>
        public object PageContent
        {
            get => _contentControl.Content;
            set => _contentControl.Content = value;
        }
        
        /// <summary>
        /// Gets the view model.
        /// </summary>
        public TViewModel ViewModel { get; }
        
        /// <summary>
        /// Gets the page title.
        /// </summary>
        public abstract string Title { get; }
        
        /// <summary>
        /// Gets the page icon.
        /// </summary>
        public abstract string Icon { get; }
        
        /// <summary>
        /// Gets the page content for navigation.
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
        /// Called when the page is navigated to.
        /// </summary>
        public virtual void OnNavigatedTo()
        {
        }
        
        /// <summary>
        /// Called when the page is navigated from.
        /// </summary>
        public virtual void OnNavigatedFrom()
        {
        }
        
        /// <summary>
        /// Called when the page is activated.
        /// </summary>
        public virtual void OnActivated()
        {
            ViewModel.Initialize();
        }
        
        /// <summary>
        /// Called when the page is deactivated.
        /// </summary>
        public virtual void OnDeactivated()
        {
            ViewModel.Cleanup();
        }
        
        /// <summary>
        /// Called when the page is refreshed.
        /// </summary>
        public virtual void OnRefresh()
        {
        }
    }
}
