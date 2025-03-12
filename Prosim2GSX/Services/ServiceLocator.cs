using System;
using Microsoft.Extensions.DependencyInjection;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service locator for resolving dependencies.
    /// </summary>
    public static class ServiceLocator
    {
        private static IServiceProvider _serviceProvider;
        private static bool _isInitialized;

        /// <summary>
        /// Initializes the service locator with the specified service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        public static void Initialize(IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _serviceProvider = services.BuildServiceProvider();
            _isInitialized = true;
        }

        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service to get.</typeparam>
        /// <returns>The service instance.</returns>
        public static T GetService<T>() where T : class
        {
            EnsureInitialized();
            return _serviceProvider.GetService<T>();
        }

        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        /// <param name="serviceType">The type of service to get.</param>
        /// <returns>The service instance.</returns>
        public static object GetService(Type serviceType)
        {
            EnsureInitialized();
            return _serviceProvider.GetService(serviceType);
        }

        /// <summary>
        /// Gets a required service of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service to get.</typeparam>
        /// <returns>The service instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the service is not registered.</exception>
        public static T GetRequiredService<T>() where T : class
        {
            EnsureInitialized();
            return _serviceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Gets a required service of the specified type.
        /// </summary>
        /// <param name="serviceType">The type of service to get.</param>
        /// <returns>The service instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the service is not registered.</exception>
        public static object GetRequiredService(Type serviceType)
        {
            EnsureInitialized();
            return _serviceProvider.GetRequiredService(serviceType);
        }

        /// <summary>
        /// Ensures that the service locator is initialized.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("ServiceLocator has not been initialized. Call Initialize first.");
            }
        }
    }
}
