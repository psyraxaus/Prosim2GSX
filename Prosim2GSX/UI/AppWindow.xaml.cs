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
            this.MaxHeight = SystemParameters.WorkArea.Height * 0.95;
            this.Loaded += OnWindowLoaded;
            this.IsVisibleChanged += OnVisibleChanged;
            this.SizeChanged += OnWindowSizeChanged;

            // Show SDK warning banner if running in degraded mode
            if (!Prosim2GSX.Instance.IsSdkAvailable)
            {
                LabelSdkWarning.Text = "ProSim SDK is not configured. ProSim integration is disabled. Please set the SDK path in App Settings and restart the application.";
                PanelSdkWarning.Visibility = Visibility.Visible;
            }

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
                        NavigateUri = new Uri("https://github.com/psyraxaus/Prosim2GSX/blob/master/Prosim2GSX-Installer-latest.exe")
                    };
                else
                    hyperlink = new Hyperlink(run)
                    {
                        NavigateUri = new Uri("https://github.com/psyraxaus/Prosim2GSX/releases/latest")
                    };
                LabelVersionCheck.Inlines.Add(hyperlink);
                LabelVersionCheck.Inlines.Add(" available!");
                this.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(Nav.RequestNavigateHandler));
                PanelVersion.Visibility = Visibility.Visible;
            }
        }

        protected virtual void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            bool sdkAvailable = Prosim2GSX.Instance.IsSdkAvailable;

            if (sdkAvailable)
            {
                MainTabControl.Items[0] = CreateTabItem("FLIGHT STATUS", new ViewMonitor());
                MainTabControl.Items[1] = CreateTabItem("OFP", new Views.Ofp.ViewOfp());
                MainTabControl.Items[2] = CreateTabItem("GSX SETTINGS", new Views.Automation.ViewAutomation());
                MainTabControl.Items[3] = CreateTabItem("AIRCRAFT PROFILES", new ViewProfiles());
                MainTabControl.Items[4] = CreateTabItem("AUDIO SETTINGS", new ViewAudio());
            }
            else
            {
                // In degraded mode, replace SDK-dependent tabs with placeholder content
                var placeholder = CreateDegradedPlaceholder();
                MainTabControl.Items[0] = CreateTabItem("FLIGHT STATUS", placeholder);
                MainTabControl.Items[1] = CreateTabItem("OFP", CreateDegradedPlaceholder());
                MainTabControl.Items[2] = CreateTabItem("GSX SETTINGS", CreateDegradedPlaceholder());
                MainTabControl.Items[3] = CreateTabItem("AIRCRAFT PROFILES", CreateDegradedPlaceholder());
                MainTabControl.Items[4] = CreateTabItem("AUDIO SETTINGS", CreateDegradedPlaceholder());
            }

            // Settings tab is always available (needed to configure SDK path)
            MainTabControl.Items[5] = CreateTabItem("APP SETTINGS", new ViewSettings());

            // Set index and previousTabIndex before subscribing so SelectionChanged
            // does not fire Start() while the window is still in its layout pass.
            int defaultTab = sdkAvailable ? 0 : 5; // Go straight to Settings in degraded mode
            MainTabControl.SelectedIndex = defaultTab;
            _previousTabIndex = defaultTab;
            MainTabControl.SelectionChanged += OnTabSelectionChanged;

            if (sdkAvailable)
            {
                // Defer the initial Start() until after the window is fully rendered
                // so backend services have a chance to initialise before polling begins.
                Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() => ((MainTabControl.Items[0] as TabItem)?.Content as IView)?.Start()));
            }
        }

        private static UIElement CreateDegradedPlaceholder()
        {
            return new TextBlock
            {
                Text = "ProSim SDK is not configured. Please set the SDK path in App Settings and restart the application.",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap,
                Foreground = System.Windows.Media.Brushes.Gray
            };
        }

        private static TabItem CreateTabItem(string header, UIElement content)
        {
            return new TabItem
            {
                Header = header,
                Content = content
            };
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!e.HeightChanged) return;

            var workArea = SystemParameters.WorkArea;
            double windowBottom = this.Top + this.ActualHeight;
            double workAreaBottom = workArea.Top + workArea.Height;

            if (windowBottom > workAreaBottom)
            {
                this.Top = Math.Max(workArea.Top, workAreaBottom - this.ActualHeight);
            }
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
