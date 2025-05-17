using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Prosim2GSX.Services.Logging
{
    /// <summary>
    /// Provides methods to configure logging for the application.
    /// </summary>
    public static class ConfigureLogging
    {
        /// <summary>
        /// Configures logging services with standard providers and common settings.
        /// </summary>
        /// <param name="services">The service collection to configure</param>
        /// <param name="configuration">Optional configuration object to use</param>
        /// <returns>The updated service collection</returns>
        public static IServiceCollection AddProsimLogging(
            this IServiceCollection services,
            Microsoft.Extensions.Configuration.IConfiguration configuration = null)
        {
            // Configure logging
            services.AddLogging(builder =>
            {
                // Add console provider for development
                builder.AddConsole();

                // Add debug provider for development
                builder.AddDebug();

                // Configure Serilog (if still using it)
                if (Serilog.Log.Logger != null)
                {
                    builder.AddSerilog(Log.Logger, dispose: false);
                }

                // Set default minimum level
                builder.SetMinimumLevel(LogLevel.Information);

                // Configure category-specific levels using strings instead of LogCategories to avoid ambiguity
                builder.AddFilter("Prosim2GSX.Refueling", LogLevel.Debug);
                builder.AddFilter("Prosim2GSX.SimConnect", LogLevel.Information);
                builder.AddFilter("Prosim2GSX.Prosim", LogLevel.Information);
                builder.AddFilter("Prosim2GSX.Menu", LogLevel.Debug);
                builder.AddFilter("Prosim2GSX.Loadsheet", LogLevel.Debug);
            });

            // Add UI logging options
            services.Configure<Options.UiLoggerOptions>(options =>
            {
                options.MaxLogEntries = 1000;
                options.ShowTimestamps = true;
                options.ShowLogLevels = true;
                options.ShowDebugMessages = false;
            });

            // Note: We're not registering UI log listener here to avoid circular dependencies
            // It will be created manually in App.xaml.cs

            return services;
        }

        /// <summary>
        /// Configures an existing LoggerFactory with providers and filters
        /// </summary>
        // In ConfigureLogging.cs, update the ConfigureLoggerFactory method
        public static void ConfigureLoggerFactory(
            Microsoft.Extensions.Logging.ILoggerFactory loggerFactory,
            Microsoft.Extensions.Logging.LogLevel minimumLevel,
            Serilog.Core.Logger serilogLogger = null)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            // Only add providers if explicitly requested - don't create duplicates
            // If we have a Serilog logger, add it but ONLY if it's not already added
            if (serilogLogger != null)
            {
                // Add Serilog provider - we can't check if it's already added, so we'll just trust the caller
                loggerFactory.AddSerilog(serilogLogger, dispose: false);
            }

            // NO LONGER using FilterLoggerProvider as it's likely causing duplicates
        }


        /// <summary>
        /// A logger provider that filters log messages based on a minimum level
        /// and delegates to other providers
        /// </summary>
        public class FilterLoggerProvider : ILoggerProvider
        {
            private readonly LogLevel _minimumLevel;
            private readonly ConcurrentDictionary<string, FilterLogger> _loggers = new ConcurrentDictionary<string, FilterLogger>();
            private readonly ILoggerProvider[] _otherProviders;

            /// <summary>
            /// Creates a new filter logger provider that delegates to other providers
            /// </summary>
            /// <param name="minimumLevel">Minimum log level to allow</param>
            /// <param name="otherProviders">Providers to delegate to</param>
            public FilterLoggerProvider(
                LogLevel minimumLevel,
                params ILoggerProvider[] otherProviders)
            {
                _minimumLevel = minimumLevel;
                _otherProviders = otherProviders ?? Array.Empty<ILoggerProvider>();
            }

            /// <summary>
            /// Creates a logger for the specified category
            /// </summary>
            /// <param name="categoryName">The category name</param>
            /// <returns>A logger instance</returns>
            public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
            {
                return _loggers.GetOrAdd(categoryName, name =>
                {
                    // Create loggers from other providers
                    var innerLoggers = new List<Microsoft.Extensions.Logging.ILogger>();
                    foreach (var provider in _otherProviders)
                    {
                        innerLoggers.Add(provider.CreateLogger(name));
                    }

                    return new FilterLogger(innerLoggers.ToArray(), _minimumLevel);
                });
            }

            /// <summary>
            /// Disposes the provider and its resources
            /// </summary>
            public void Dispose()
            {
                // Dispose other providers
                foreach (var provider in _otherProviders)
                {
                    provider?.Dispose();
                }

                _loggers.Clear();
            }

            /// <summary>
            /// Logger implementation that filters messages and delegates to inner loggers
            /// </summary>
            private class FilterLogger : Microsoft.Extensions.Logging.ILogger
            {
                private readonly Microsoft.Extensions.Logging.ILogger[] _innerLoggers;
                private readonly LogLevel _minimumLevel;

                /// <summary>
                /// Creates a new filter logger
                /// </summary>
                /// <param name="innerLoggers">Loggers to delegate to</param>
                /// <param name="minimumLevel">Minimum log level</param>
                public FilterLogger(
                    Microsoft.Extensions.Logging.ILogger[] innerLoggers,
                    LogLevel minimumLevel)
                {
                    _innerLoggers = innerLoggers ?? Array.Empty<Microsoft.Extensions.Logging.ILogger>();
                    _minimumLevel = minimumLevel;
                }

                /// <summary>
                /// Begins a logical operation scope
                /// </summary>
                /// <typeparam name="TState">The type of the state object</typeparam>
                /// <param name="state">The state object</param>
                /// <returns>A disposable scope object</returns>
                public IDisposable BeginScope<TState>(TState state)
                {
                    // Create a composite disposable for all scopes
                    var disposables = new List<IDisposable>();
                    foreach (var logger in _innerLoggers)
                    {
                        var scope = logger.BeginScope(state);
                        if (scope != null)
                        {
                            disposables.Add(scope);
                        }
                    }

                    return new CompositeDisposable(disposables);
                }

                /// <summary>
                /// Checks if logging is enabled for the specified level
                /// </summary>
                /// <param name="logLevel">The log level to check</param>
                /// <returns>True if logging is enabled</returns>
                public bool IsEnabled(LogLevel logLevel)
                {
                    if (logLevel < _minimumLevel)
                        return false;

                    // If any inner logger is enabled, we're enabled
                    foreach (var logger in _innerLoggers)
                    {
                        if (logger.IsEnabled(logLevel))
                            return true;
                    }

                    return false;
                }

                /// <summary>
                /// Logs a message at the specified level
                /// </summary>
                /// <typeparam name="TState">The type of the state object</typeparam>
                /// <param name="logLevel">The log level</param>
                /// <param name="eventId">The event ID</param>
                /// <param name="state">The state object</param>
                /// <param name="exception">Optional exception</param>
                /// <param name="formatter">Message formatter function</param>
                public void Log<TState>(
                    LogLevel logLevel,
                    EventId eventId,
                    TState state,
                    Exception exception,
                    Func<TState, Exception, string> formatter)
                {
                    if (logLevel < _minimumLevel)
                        return;

                    // Log to all inner loggers
                    foreach (var logger in _innerLoggers)
                    {
                        if (logger.IsEnabled(logLevel))
                        {
                            logger.Log(logLevel, eventId, state, exception, formatter);
                        }
                    }
                }

                /// <summary>
                /// A disposable that disposes multiple resources
                /// </summary>
                private class CompositeDisposable : IDisposable
                {
                    private readonly List<IDisposable> _disposables;

                    /// <summary>
                    /// Creates a new composite disposable
                    /// </summary>
                    /// <param name="disposables">Disposables to manage</param>
                    public CompositeDisposable(List<IDisposable> disposables)
                    {
                        _disposables = disposables;
                    }

                    /// <summary>
                    /// Disposes all managed resources
                    /// </summary>
                    public void Dispose()
                    {
                        foreach (var disposable in _disposables)
                        {
                            disposable?.Dispose();
                        }
                    }
                }
            }
        }
    }
}
