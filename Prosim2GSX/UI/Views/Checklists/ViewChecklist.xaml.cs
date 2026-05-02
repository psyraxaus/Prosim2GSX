using System.Windows.Controls;

namespace Prosim2GSX.UI.Views.Checklists
{
    public partial class ViewChecklist : UserControl, IView
    {
        protected virtual ModelChecklist ViewModel { get; }

        public ViewChecklist()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;
            ViewModel.FocusSectionDropdownRequested += OnFocusSectionDropdown;
        }

        private void OnFocusSectionDropdown()
        {
            try
            {
                SectionCombo.IsDropDownOpen = true;
                SectionCombo.Focus();
            }
            catch { }
        }

        public virtual void Start() => ViewModel.Start();
        public virtual void Stop() => ViewModel.Stop();
    }
}
