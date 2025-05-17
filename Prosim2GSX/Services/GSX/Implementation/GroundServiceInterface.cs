using Microsoft.Extensions.Logging;
using Prosim2GSX.Services.GSX.Interfaces;
using Prosim2GSX.Services.GSX.Models;
using Prosim2GSX.Services.Prosim.Interfaces;

namespace Prosim2GSX.Services.GSX.Implementation
{
    /// <summary>
    /// Implementation of ground service operations
    /// </summary>
    public class GroundServiceImplementation : IGroundServiceInterface
    {
        private readonly IProsimInterface _prosimInterface;
        private readonly ILogger<GroundServiceImplementation> _logger;

        /// <summary>
        /// Creates a new instance of the ground service implementation
        /// </summary>
        /// <param name="logger">Logger for this service</param>
        /// <param name="prosimInterface">ProSim interface for accessing variables</param>
        public GroundServiceImplementation(
            ILogger<GroundServiceImplementation> logger,
            IProsimInterface prosimInterface)
        {
            _logger = logger;
            _prosimInterface = prosimInterface;

            _logger.LogDebug("Ground Service Interface initialized");
        }

        /// <inheritdoc/>
        public void SetChocks(bool enable)
        {
            _logger.LogInformation("Setting chocks: {ChocksEnabled}", enable);
            _prosimInterface.SetProsimVariable("efb.chocks", enable);
        }

        /// <inheritdoc/>
        public void SetGPU(bool enable)
        {
            _logger.LogInformation("Setting GPU: {GPUEnabled}", enable);
            _prosimInterface.SetProsimVariable("groundservice.groundpower", enable);
        }

        /// <inheritdoc/>
        public void SetPCA(bool enable)
        {
            _logger.LogInformation("Setting PCA: {PCAEnabled}", enable);
            _prosimInterface.SetProsimVariable("groundservice.preconditionedAir", enable);
        }

        public GroundServiceStatus GetStatus()
        {
            _logger.LogDebug("Getting ground service status");

            var status = new GroundServiceStatus
            {
                ChocksSet = (bool)_prosimInterface.GetProsimVariable("efb.chocks"),
                GPUConnected = (bool)_prosimInterface.GetProsimVariable("groundservice.groundpower"),
                PCAConnected = (bool)_prosimInterface.GetProsimVariable("groundservice.preconditionedAir")
            };

            _logger.LogDebug("Ground service status: Chocks={ChocksSet}, GPU={GPUConnected}, PCA={PCAConnected}",
                status.ChocksSet, status.GPUConnected, status.PCAConnected);

            return status;
        }
    }
}
