using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for refueling commands
    /// </summary>
    public interface IRefuelingCommand
    {
        /// <summary>
        /// Executes the command asynchronously
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the command was executed successfully, false otherwise</returns>
        Task<bool> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
