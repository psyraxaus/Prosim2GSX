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

        /// <summary>
        /// Creates a new instance of the ground service implementation
        /// </summary>
        /// <param name="prosimInterface">ProSim interface for accessing variables</param>
        public GroundServiceImplementation(IProsimInterface prosimInterface)
        {
            _prosimInterface = prosimInterface;
        }

        /// <inheritdoc/>
        public void SetChocks(bool enable)
        {
            _prosimInterface.SetProsimVariable("efb.chocks", enable);
        }

        /// <inheritdoc/>
        public void SetGPU(bool enable)
        {
            _prosimInterface.SetProsimVariable("groundservice.groundpower", enable);
        }

        /// <inheritdoc/>
        public void SetPCA(bool enable)
        {
            _prosimInterface.SetProsimVariable("groundservice.preconditionedAir", enable);
        }

        public GroundServiceStatus GetStatus()
        {
            return new GroundServiceStatus
            {
                ChocksSet = (bool)_prosimInterface.GetProsimVariable("efb.chocks"),
                GPUConnected = (bool)_prosimInterface.GetProsimVariable("groundservice.groundpower"),
                PCAConnected = (bool)_prosimInterface.GetProsimVariable("groundservice.preconditionedAir")
            };
        }
    }
}