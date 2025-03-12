﻿using System;
using System.Collections;
using System.Globalization;
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
            // Determine context length
            ReadOnlySpan<char> contextSpan = context.AsSpan();
            ReadOnlySpan<char> formattedContext = contextSpan.Length <= 32 ? 
                contextSpan : contextSpan.Slice(0, 32);
            
            // Clean the message by removing newlines and tabs
            string cleanMessage = message.Replace("\n", "").Replace("\r", "").Replace("\t", "");
            
            // Calculate buffer size with extra padding to ensure enough space
            int bufferSize = formattedContext.Length + cleanMessage.Length + 50; // Add more padding for safety
            
            // Use stackalloc for small buffers to avoid heap allocations
            Span<char> buffer = bufferSize <= 256 ? 
                stackalloc char[bufferSize] : // Use stack for small buffers
                new char[bufferSize];         // Use heap for large buffers
            
            // Format the log entry without string allocations
            int position = 0;
            
            // Add prefix "[ "
            "[ ".AsSpan().CopyTo(buffer.Slice(position));
            position += 2;
            
            // Add formatted context
            formattedContext.CopyTo(buffer.Slice(position));
            position += formattedContext.Length;
            
            // Add padding spaces to align to 32 characters
            int padding = Math.Max(0, 32 - formattedContext.Length);
            for (int i = 0; i < padding; i++)
            {
                buffer[position++] = ' ';
            }
            
            // Add separator " ] "
            " ] ".AsSpan().CopyTo(buffer.Slice(position));
            position += 3;
            
            // Add clean message
            cleanMessage.AsSpan().CopyTo(buffer.Slice(position));
            position += cleanMessage.Length;
            
            // Convert to string only once for the final log entry
            string entry = buffer.Slice(0, position).ToString();
            
            // Log using Serilog as before
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
            // Create exception message
            string exceptionMessage = message != null 
                ? $"{message}: {exception.Message}" 
                : exception.Message;
            
            // Determine context length
            ReadOnlySpan<char> contextSpan = context.AsSpan();
            ReadOnlySpan<char> formattedContext = contextSpan.Length <= 32 ? 
                contextSpan : contextSpan.Slice(0, 32);
            
            // Clean the message by removing newlines and tabs
            string cleanMessage = exceptionMessage.Replace("\n", "").Replace("\r", "").Replace("\t", "");
            
            // Calculate buffer size with extra padding to ensure enough space
            int bufferSize = formattedContext.Length + cleanMessage.Length + 50; // Add more padding for safety
            
            // Use stackalloc for small buffers to avoid heap allocations
            Span<char> buffer = bufferSize <= 256 ? 
                stackalloc char[bufferSize] : // Use stack for small buffers
                new char[bufferSize];         // Use heap for large buffers
            
            // Format the log entry without string allocations
            int position = 0;
            
            // Add prefix "[ "
            "[ ".AsSpan().CopyTo(buffer.Slice(position));
            position += 2;
            
            // Add formatted context
            formattedContext.CopyTo(buffer.Slice(position));
            position += formattedContext.Length;
            
            // Add padding spaces to align to 32 characters
            int padding = Math.Max(0, 32 - formattedContext.Length);
            for (int i = 0; i < padding; i++)
            {
                buffer[position++] = ' ';
            }
            
            // Add separator " ] "
            " ] ".AsSpan().CopyTo(buffer.Slice(position));
            position += 3;
            
            // Add clean message
            cleanMessage.AsSpan().CopyTo(buffer.Slice(position));
            position += cleanMessage.Length;
            
            // Convert to string only once for the final log entry
            string entry = buffer.Slice(0, position).ToString();
                
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
