using System;
using Prosim2GSX.UI.EFB.Navigation;

namespace Prosim2GSX.UI.EFB.Factories
{
    /// <summary>
    /// A simple implementation of IEFBPageFactory that creates pages using Activator.CreateInstance.
    /// </summary>
    public class SimpleEFBPageFactory : IEFBPageFactory
    {
        /// <summary>
        /// Creates a page instance of the specified type.
        /// </summary>
        /// <param name="pageType">The type of page to create.</param>
        /// <returns>The created page instance.</returns>
        public IEFBPage CreatePage(Type pageType)
        {
            if (pageType == null)
            {
                throw new ArgumentNullException(nameof(pageType));
            }

            // Check if the type has a parameterless constructor
            if (pageType.GetConstructor(Type.EmptyTypes) != null)
            {
                return (IEFBPage)Activator.CreateInstance(pageType);
            }

            // If we get here, we don't know how to create the page
            throw new InvalidOperationException($"No factory method available for page type {pageType.Name}");
        }
    }
}
