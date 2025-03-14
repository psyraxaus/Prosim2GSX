using System;
using Prosim2GSX.UI.EFB.ViewModels;

namespace Prosim2GSX.UI.EFB.Views
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : HomePageBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HomePage"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public HomePage(HomeViewModel viewModel) : base(viewModel)
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// Gets the page title.
        /// </summary>
        public override string Title => "Home";
        
        /// <summary>
        /// Gets the page icon.
        /// </summary>
        public override string Icon => "&#xE80F;";
        
        /// <summary>
        /// Called when the page is navigated to.
        /// </summary>
        public override void OnNavigatedTo()
        {
            base.OnNavigatedTo();
            
            // Update the view model
            ViewModel.Initialize();
        }
        
        /// <summary>
        /// Called when the page is refreshed.
        /// </summary>
        public override void OnRefresh()
        {
            base.OnRefresh();
            
            // Update the view model
            ViewModel.Initialize();
        }
    }
}
