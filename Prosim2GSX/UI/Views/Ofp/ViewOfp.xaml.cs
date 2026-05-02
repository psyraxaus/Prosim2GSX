using System.Windows.Controls;

namespace Prosim2GSX.UI.Views.Ofp
{
    public partial class ViewOfp : UserControl, IView
    {
        protected virtual ModelOfp ViewModel { get; }

        public ViewOfp()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;
        }

        public virtual void Start()
        {
            ViewModel.OnTabActivated();
        }

        public virtual void Stop()
        {
        }
    }
}
