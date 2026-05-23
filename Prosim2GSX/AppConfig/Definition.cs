using CFIT.AppFramework.AppConfig;
using System.IO;

namespace Prosim2GSX.AppConfig
{
    public class Definition : ProductDefinitionBase
    {
        public override int BuildConfigVersion { get; } = 32;
        public override string ProductName => "Prosim2GSX";
        public override string ProductExePath => Path.Join(Path.Join(ProductPath, "bin"), ProductExe);
        public override bool ProductVersionCheckDev => true;
        public override bool RequireSimRunning => false;
        public override bool WaitForSim => true;
        public override bool SingleInstance => true;
        // StressMode short-circuits to false regardless of the normal flags
        // so ground reproduction of the ERROR_NOT_ENOUGH_QUOTA crash always
        // runs headless (no WPF window). Outside StressMode the original
        // OpenAppWindowOnStart/ForceOpen logic is preserved.
        public override bool MainWindowShowOnStartup =>
            AppService.Instance?.Config?.StressMode != true
            && (AppService.Instance?.Config?.OpenAppWindowOnStart == true
                || AppService.Instance?.Config?.ForceOpen == true);
    }
}
