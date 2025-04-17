using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Windows.Media;

namespace Prosim2GSX
{
    public enum LogLevel
    {
        Critical = 5,
        Error = 4,
        Warning = 3,
        Information = 2,
        Debug = 1,
        Verbose = 0,
    }

    public class LogEntry
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Context { get; set; }

        public LogEntry(string message, LogLevel level, string context)
        {
            Message = message;
            Timestamp = DateTime.Now;
            Level = level;
            Context = context;
        }

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] {Message}";
        }

        public Brush LevelBrush
        {
            get
            {
                return Level switch
                {
                    LogLevel.Critical => new SolidColorBrush(Colors.Red),
                    LogLevel.Error => new SolidColorBrush(Colors.OrangeRed),
                    LogLevel.Warning => new SolidColorBrush(Colors.Orange),
                    LogLevel.Information => new SolidColorBrush(Colors.Black),
                    LogLevel.Debug => new SolidColorBrush(Colors.Gray),
                    LogLevel.Verbose => new SolidColorBrush(Colors.DarkGray),
                    _ => new SolidColorBrush(Colors.Black)
                };
            }
        }
    }

    public static class Logger
    {
        // For backward compatibility
        public static Queue MessageQueue = new();
        
        // New queue for LogEntry objects
        public static ConcurrentQueue<LogEntry> LogEntryQueue = new ConcurrentQueue<LogEntry>();

        public static void Log(LogLevel level, string context, string message)
        {
            // Create a LogEntry object
            var logEntry = new LogEntry(message, level, context);
            
            // Format for Serilog (keeping existing functionality)
            string entry = string.Format("[ {0,-32} ] {1}", (context.Length <= 32 ? context : context[0..32]), message.Replace("\n", "").Replace("\r", "").Replace("\t", ""));
            
            switch (level)
            {
                case LogLevel.Critical:
                    Serilog.Log.Logger.Fatal(entry);
                    break;
                case LogLevel.Error:
                    Serilog.Log.Logger.Error(entry);
                    break;
                case LogLevel.Warning:
                    Serilog.Log.Logger.Warning(entry);
                    break;
                case LogLevel.Information:
                    Serilog.Log.Logger.Information(entry);
                    break;
                case LogLevel.Debug:
                    Serilog.Log.Logger.Debug(entry);
                    break;
                case LogLevel.Verbose:
                    Serilog.Log.Logger.Verbose(entry);
                    break;
                default:
                    Serilog.Log.Logger.Debug(entry);
                    break;
            }
            
            if (level != LogLevel.Debug)
            {
                // Add to both queues for backward compatibility
                MessageQueue.Enqueue(message);
                LogEntryQueue.Enqueue(logEntry);
            }
        }
    }
}
