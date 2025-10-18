using CFIT.AppFramework;
using CFIT.AppLogger;
using Prosim2GSX.AppConfig;
using Prosim2GSX.UI;
using Prosim2GSX.UI.NotifyIcon;
using System;

namespace Prosim2GSX
{
    public class Prosim2GSX(Type windowType) : SimApp<Prosim2GSX, AppService, Config, Definition>(windowType, typeof(NotifyIconModelExt))
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                var app = new Prosim2GSX(typeof(AppWindow));
                return app.Start(args);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return -1;
            }
        }

        protected override void InitAppWindow()
        {
            base.InitAppWindow();
            AppContext.SetSwitch("Switch.System.Windows.Controls.Grid.StarDefinitionsCanExceedAvailableSpace", true);
        }
    }
}
