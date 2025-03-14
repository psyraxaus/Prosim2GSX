using Prosim2GSX.UI.EFB.ViewModels;

namespace Prosim2GSX.UI.EFB.Views
{
    /// <summary>
    /// Non-generic base class for HomePage to solve XAML generic type issues.
    /// </summary>
    public abstract class HomePageBase : BasePage<HomeViewModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HomePageBase"/> class.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        public HomePageBase(HomeViewModel viewModel) : base(viewModel)
        {
        }

        /// <summary>
        /// Gets the page title.
        /// </summary>
        public override abstract string Title { get; }
        
        /// <summary>
        /// Gets the page icon.
        /// </summary>
        public override abstract string Icon { get; }
    }
}
