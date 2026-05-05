using System.Windows;
using System.Windows.Controls;

namespace Prosim2GSX.UI.Views.Loadsheet
{
    public partial class ViewLoadsheet : UserControl, IView
    {
        protected virtual ModelLoadsheet ViewModel { get; }

        public ViewLoadsheet()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;
        }

        // No per-tab Start/Stop work — the view-model is a notification adapter
        // over LoadsheetState, which is populated continuously by
        // StateUpdateWorker regardless of which tab is open.
        public virtual void Start()
        {
        }

        public virtual void Stop()
        {
        }

        protected virtual void OnResendClick(object sender, RoutedEventArgs e)
        {
            ViewModel?.OnResend();
        }

        protected virtual void OnResetClick(object sender, RoutedEventArgs e)
        {
            // Confirm before resetting both slots — same UX as the web panel.
            // GetWindow null-fallback is fine: MessageBox.Show without an owner
            // uses the active window.
            var owner = Window.GetWindow(this);
            var result = MessageBox.Show(
                owner,
                "Reset both PRELIM and FINAL loadsheet slots?",
                "Reset Loadsheets",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            if (result == MessageBoxResult.Yes)
                ViewModel?.OnReset();
        }
    }
}
