using Prosim2GSX.Services.Logger.Enums;
using System.Windows.Media;

namespace Prosim2GSX.Services.Logger.Implementation
{
    /// <summary>
    /// Extension methods for the LogEntry class
    /// </summary>
    public static class LogEntryExtensions
    {
        /// <summary>
        /// Gets a brush color based on the log level
        /// </summary>
        public static Brush GetLevelBrush(this LogEntry entry)
        {
            switch (entry.Level)
            {
                case LogLevel.Critical:
                    return new SolidColorBrush(Colors.DarkRed);
                case LogLevel.Error:
                    return new SolidColorBrush(Colors.Red);
                case LogLevel.Warning:
                    return new SolidColorBrush(Colors.Orange);
                case LogLevel.Information:
                    return new SolidColorBrush(Colors.Black);
                case LogLevel.Debug:
                    return new SolidColorBrush(Colors.Gray);
                case LogLevel.Verbose:
                    return new SolidColorBrush(Colors.LightGray);
                default:
                    return new SolidColorBrush(Colors.Black);
            }
        }
    }
}
