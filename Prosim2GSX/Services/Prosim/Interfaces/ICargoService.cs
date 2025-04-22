namespace Prosim2GSX.Services.Prosim.Interfaces
{
    /// <summary>
    /// Service for managing cargo in ProSim
    /// </summary>
    public interface ICargoService
    {
        /// <summary>
        /// Planned cargo amount
        /// </summary>
        int PlannedCargo { get; }

        /// <summary>
        /// Update cargo loading
        /// </summary>
        /// <param name="cargoPercentage">Percentage (0-100) of cargo to load</param>
        void UpdateCargoLoading(int cargoPercentage);

        /// <summary>
        /// Update cargo data from the flight plan
        /// </summary>
        /// <param name="flightPlan">The flight plan</param>
        void UpdateCargoData(FlightPlan flightPlan);
    }
}