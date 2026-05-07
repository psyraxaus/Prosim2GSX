using System.Windows;
using System.Windows.Controls;

namespace Prosim2GSX.UI.Views.Init
{
    public partial class ViewInit : UserControl, IView
    {
        protected virtual ModelInit ViewModel { get; }

        public ViewInit()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;
        }

        // No per-tab Start/Stop — the view-model is a notification adapter
        // over EfbFlightPlanState, which is populated continuously by
        // EfbFlightPlanService regardless of whether the tab is active.
        public virtual void Start() { }
        public virtual void Stop() { }

        protected virtual void OnFetchOfpClick(object sender, RoutedEventArgs e)
            => ViewModel?.OnFetchOfp();

        protected virtual void OnSyncToFmsClick(object sender, RoutedEventArgs e)
            => ViewModel?.OnSyncToFms();

        protected virtual void OnClearOverridesClick(object sender, RoutedEventArgs e)
            => ViewModel?.OnClearOverrides();

        protected virtual void OnResetFlightClick(object sender, RoutedEventArgs e)
        {
            // Same confirmation pattern as ViewLoadsheet.OnResetClick — the
            // reset is destructive (clears OFP + all overrides), so we
            // require an explicit yes before firing.
            var owner = Window.GetWindow(this);
            var result = MessageBox.Show(
                owner,
                "Reset the flight plan? This clears the OFP cache and all overrides.",
                "Reset Flight",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            if (result == MessageBoxResult.Yes)
                ViewModel?.OnResetFlight();
        }
    }
}
