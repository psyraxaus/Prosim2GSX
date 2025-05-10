using System;

namespace Prosim2GSX.Services.GSX.Interfaces
{
    /// <summary>
    /// Service for GSX SimConnect integration
    /// </summary>
    public interface IGsxSimConnectService
    {
        /// <summary>
        /// Read a GSX LVAR
        /// </summary>
        /// <param name="lvarName">LVAR name</param>
        /// <returns>LVAR value</returns>
        float ReadGsxLvar(string lvarName);

        /// <summary>
        /// Write a GSX LVAR
        /// </summary>
        /// <param name="lvarName">LVAR name</param>
        /// <param name="value">Value to write</param>
        void WriteGsxLvar(string lvarName, float value);

        /// <summary>
        /// Subscribe to GSX LVAR changes
        /// </summary>
        /// <param name="lvarName">LVAR name</param>
        /// <param name="handler">Handler to call when LVAR changes</param>
        void SubscribeToGsxLvar(string lvarName, Action<float, float, string> handler);

        /// <summary>
        /// Check if simulator is on ground
        /// </summary>
        /// <returns>True if on ground</returns>
        bool IsSimOnGround();

        /// <summary>
        /// Check if Couatl is running
        /// </summary>
        /// <returns>True if running</returns>
        bool IsCouatlRunning();

        /// <summary>
        /// Get current departure state
        /// </summary>
        /// <returns>Departure state (0-6)</returns>
        int GetDepartureState();

        /// <summary>
        /// Get current catering state
        /// </summary>
        /// <returns>Catering state (0-6)</returns>
        int GetCateringState();

        /// <summary>
        /// Get current boarding state
        /// </summary>
        /// <returns>Boarding state (0-6)</returns>
        int GetBoardingState();

        /// <summary>
        /// Get current deboarding state
        /// </summary>
        /// <returns>Deboarding state (0-6)</returns>
        int GetDeboardingState();

        /// <summary>
        /// Check if fuel hose is connected
        /// </summary>
        /// <returns>True if connected</returns>
        bool IsFuelHoseConnected();

        /// <summary>
        /// Get passenger count boarded
        /// </summary>
        /// <returns>Number of passengers boarded</returns>
        int GetBoardedPassengerCount();

        /// <summary>
        /// Get passenger count deboarded
        /// </summary>
        /// <returns>Number of passengers deboarded</returns>
        int GetDeboardedPassengerCount();

        /// <summary>
        /// Get cargo boarding percentage
        /// </summary>
        /// <returns>Cargo boarding percentage (0-100)</returns>
        int GetCargoBoardingPercentage();

        /// <summary>
        /// Get cargo deboarding percentage
        /// </summary>
        /// <returns>Cargo deboarding percentage (0-100)</returns>
        int GetCargoDeboardingPercentage();

        /// <summary>
        /// Get current refueling state
        /// </summary>
        /// <returns>Refueling state (0-6)</returns>
        int GetRefuelingState();
    }
}