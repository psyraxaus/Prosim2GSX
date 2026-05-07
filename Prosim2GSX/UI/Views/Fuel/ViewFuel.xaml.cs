using System.Windows.Controls;

namespace Prosim2GSX.UI.Views.Fuel
{
    public partial class ViewFuel : UserControl, IView
    {
        protected virtual ModelFuel ViewModel { get; }

        public ViewFuel()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;
        }

        // No per-tab Start/Stop work — the view-model is a notification adapter
        // over FuelState, which is populated continuously by StateUpdateWorker
        // regardless of which tab is open.
        public virtual void Start()
        {
        }

        public virtual void Stop()
        {
        }
    }
}
