using Prosim2GSX.UI;
using Prosim2GSX.UI.Views.Automation;
using System.Windows.Controls;

namespace Prosim2GSX.UI.Views.GsxSettings
{
    public partial class ViewGsxSettings : UserControl, IView
    {
        protected virtual ModelAutomation ViewModel { get; }

        public ViewGsxSettings(ModelAutomation viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;

            ViewModel.BindStringNumber(nameof(ViewModel.RefuelRateKgSec), InputGsxRefuelRate, "28");
        }

        public virtual void Start() { }
        public virtual void Stop() { }
    }
}
