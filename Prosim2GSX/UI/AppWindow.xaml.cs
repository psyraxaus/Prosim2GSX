using CFIT.AppTools;
using Prosim2GSX.UI.Views.Audio;
using Prosim2GSX.UI.Views.Monitor;
using Prosim2GSX.UI.Views.Profiles;
using Prosim2GSX.UI.Views.Settings;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace Prosim2GSX.UI
{
    public interface IView
    {
        public void Start();
        public void Stop();
    }

    public partial class AppWindow : Window
    {
        public static UiIconLoader IconLoader { get; } = new(Assembly.GetExecutingAssembly(), IconLoadSource.Embedded, "Prosim2GSX.UI.Icons.");

        private int _previousTabIndex = -1;

        public AppWindow()
        {
            InitializeComponent();
            this.Loaded += OnWindowLoaded;
            this.IsVisibleChanged += OnVisibleChanged;

            if (Prosim2GSX.Instance.UpdateDetected)
            {
                if (Prosim2GSX.Instance.UpdateIsDev)
                    LabelVersionCheck.Inlines.Add("New Develop Version ");
                else
                    LabelVersionCheck.Inlines.Add("New Stable Version ");
                var run = new Run($"{Prosim2GSX.Instance.UpdateVersion}");

                Hyperlink hyperlink;
                if (Prosim2GSX.Instance.UpdateIsDev)
                    hyperlink = new Hyperlink(run)
                    {
                        NavigateUri = new Uri("https://github.com/Fragtality/Prosim2GSX/blob/master/Prosim2GSX-Installer-latest.exe")
                    };
                else
                    hyperlink = new Hyperlink(run)
                    {
                        NavigateUri = new Uri("https://github.com/Fragtality/Prosim2GSX/releases/latest")
                    };
                LabelVersionCheck.Inlines.Add(hyperlink);
                LabelVersionCheck.Inlines.Add(" available!");
                this.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(Nav.RequestNavigateHandler));
                PanelVersion.Visibility = Visibility.Visible;
            }
        }

        protected virtual void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            MainTabControl.Items[0] = CreateTabItem("FLIGHT STATUS", new ViewMonitor());
            MainTabControl.Items[1] = CreateTabItem("AUTOMATION", new Views.Automation.ViewAutomation());
            MainTabControl.Items[2] = CreateTabItem("AIRCRAFT PROFILES", new ViewProfiles());
            MainTabControl.Items[3] = CreateTabItem("AUDIO SETTINGS", new ViewAudio());
            MainTabControl.Items[4] = CreateTabItem("APP SETTINGS", new ViewSettings());

            // Set index and previousTabIndex before subscribing so SelectionChanged
            // does not fire Start() while the window is still in its layout pass.
            MainTabControl.SelectedIndex = 0;
            _previousTabIndex = 0;
            MainTabControl.SelectionChanged += OnTabSelectionChanged;

            // Defer the initial Start() until after the window is fully rendered
            // so backend services have a chance to initialise before polling begins.
            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => ((MainTabControl.Items[0] as TabItem)?.Content as IView)?.Start()));
        }

        private static TabItem CreateTabItem(string header, UIElement content)
        {
            return new TabItem
            {
                Header = header,
                Content = content
            };
        }

        protected virtual void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var selectedContent = (MainTabControl?.SelectedItem as TabItem)?.Content as IView;
            if (this.Visibility != Visibility.Visible)
                selectedContent?.Stop();
            else
                selectedContent?.Start();
        }

        protected virtual void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_previousTabIndex >= 0 && _previousTabIndex < MainTabControl.Items.Count)
            {
                var prevContent = (MainTabControl.Items[_previousTabIndex] as TabItem)?.Content as IView;
                prevContent?.Stop();
            }

            var newContent = (MainTabControl?.SelectedItem as TabItem)?.Content as IView;
            newContent?.Start();

            _previousTabIndex = MainTabControl?.SelectedIndex ?? -1;
        }
    }
}
