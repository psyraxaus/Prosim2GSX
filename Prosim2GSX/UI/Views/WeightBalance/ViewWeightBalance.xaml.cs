using System.Windows.Controls;

namespace Prosim2GSX.UI.Views.WeightBalance
{
    public partial class ViewWeightBalance : UserControl, IView
    {
        protected virtual ModelWeightBalance ViewModel { get; }

        public ViewWeightBalance()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;
        }

        // No per-tab Start/Stop work — the view-model is a notification adapter
        // over WeightBalanceState, which is populated continuously by
        // StateUpdateWorker regardless of which tab is open.
        public virtual void Start()
        {
        }

        public virtual void Stop()
        {
        }
    }
}
