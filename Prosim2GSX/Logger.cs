﻿using System;
using System.Collections;
using Prosim2GSX.Services;

namespace Prosim2GSX
{
    public static class Logger
    {
        private static readonly LoggerImplementation _instance = new LoggerImplementation();
        
        /// <summary>
        /// Gets the singleton instance of the logger
        /// </summary>
        public static ILogger Instance => _instance;
        
        public static Queue MessageQueue = new();

        /// <summary>
        /// Logs a message with the specified level, context, and message
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="context">The context of the log message</param>
        /// <param name="message">The log message</param>
        public static void Log(LogLevel level, string context, string message)
        {
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
                default:
                    Serilog.Log.Logger.Debug(entry);
                    break;
            }
            if (level != LogLevel.Debug)
                MessageQueue.Enqueue(message);
        }
        
        /// <summary>
        /// Logs an exception with the specified level, context, and optional message
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="context">The context of the log message</param>
        /// <param name="exception">The exception to log</param>
        /// <param name="message">An optional message to include with the exception</param>
        public static void Log(LogLevel level, string context, Exception exception, string message = null)
        {
            string exceptionMessage = message != null 
                ? $"{message}: {exception.Message}" 
                : exception.Message;
                
            string entry = string.Format("[ {0,-32} ] {1}", 
                (context.Length <= 32 ? context : context[0..32]), 
                exceptionMessage.Replace("\n", "").Replace("\r", "").Replace("\t", ""));
                
            switch (level)
            {
                case LogLevel.Critical:
                    Serilog.Log.Logger.Fatal(exception, entry);
                    break;
                case LogLevel.Error:
                    Serilog.Log.Logger.Error(exception, entry);
                    break;
                case LogLevel.Warning:
                    Serilog.Log.Logger.Warning(exception, entry);
                    break;
                case LogLevel.Information:
                    Serilog.Log.Logger.Information(exception, entry);
                    break;
                case LogLevel.Debug:
                    Serilog.Log.Logger.Debug(exception, entry);
                    break;
                default:
                    Serilog.Log.Logger.Debug(exception, entry);
                    break;
            }
            
            if (level != LogLevel.Debug)
                MessageQueue.Enqueue(exceptionMessage);
        }
        
        /// <summary>
        /// Implementation of the ILogger interface that uses the static Logger methods
        /// </summary>
        private class LoggerImplementation : ILogger
        {
            /// <summary>
            /// Logs a message with the specified level, context, and message
            /// </summary>
            /// <param name="level">The log level</param>
            /// <param name="source">The source of the log message</param>
            /// <param name="message">The log message</param>
            public void Log(LogLevel level, string source, string message)
            {
                Logger.Log(level, source, message);
            }
            
            /// <summary>
            /// Logs an exception with the specified level, context, and optional message
            /// </summary>
            /// <param name="level">The log level</param>
            /// <param name="source">The source of the log message</param>
            /// <param name="exception">The exception to log</param>
            /// <param name="message">An optional message to include with the exception</param>
            public void Log(LogLevel level, string source, Exception exception, string message = null)
            {
                Logger.Log(level, source, exception, message);
            }
        }
    }
}
