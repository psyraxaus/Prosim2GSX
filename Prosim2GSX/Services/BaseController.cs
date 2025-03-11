using System;
using System.Threading.Tasks;
using Prosim2GSX.Models;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Base class for all controllers
    /// </summary>
    public abstract class BaseController : IDisposable
    {
        /// <summary>
        /// Gets the service model
        /// </summary>
        protected readonly ServiceModel Model;
        
        /// <summary>
        /// Gets the logger
        /// </summary>
        protected readonly ILogger Logger;
        
        /// <summary>
        /// Gets the event aggregator
        /// </summary>
        protected readonly IEventAggregator EventAggregator;
        
        /// <summary>
        /// Gets a value indicating whether the controller is disposed
        /// </summary>
        protected bool IsDisposed;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseController"/> class
        /// </summary>
        /// <param name="model">The service model</param>
        /// <param name="logger">The logger</param>
        /// <param name="eventAggregator">The event aggregator</param>
        protected BaseController(ServiceModel model, ILogger logger, IEventAggregator eventAggregator)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        }
        
        /// <summary>
        /// Executes an action safely with error handling
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="operationName">The name of the operation for logging</param>
        protected void ExecuteSafely(Action action, string operationName)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"{GetType().Name}:{operationName}", $"Error: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Executes a function safely with error handling
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="func">The function to execute</param>
        /// <param name="operationName">The name of the operation for logging</param>
        /// <returns>The result of the function</returns>
        protected T ExecuteSafely<T>(Func<T> func, string operationName)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"{GetType().Name}:{operationName}", $"Error: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Executes an async action safely with error handling
        /// </summary>
        /// <param name="action">The async action to execute</param>
        /// <param name="operationName">The name of the operation for logging</param>
        /// <returns>A task representing the asynchronous operation</returns>
        protected async Task ExecuteSafelyAsync(Func<Task> action, string operationName)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"{GetType().Name}:{operationName}", $"Error: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Executes an async function safely with error handling
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="func">The async function to execute</param>
        /// <param name="operationName">The name of the operation for logging</param>
        /// <returns>A task representing the asynchronous operation with the result</returns>
        protected async Task<T> ExecuteSafelyAsync<T>(Func<Task<T>> func, string operationName)
        {
            try
            {
                return await func();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"{GetType().Name}:{operationName}", $"Error: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Initializes the controller
        /// </summary>
        public virtual void Initialize()
        {
            Logger.Log(LogLevel.Information, $"{GetType().Name}:Initialize", "Initializing controller");
        }
        
        /// <summary>
        /// Disposes resources used by the controller
        /// </summary>
        public virtual void Dispose()
        {
            if (IsDisposed)
                return;
                
            Logger.Log(LogLevel.Information, $"{GetType().Name}:Dispose", "Disposing controller");
            IsDisposed = true;
        }
    }
}
