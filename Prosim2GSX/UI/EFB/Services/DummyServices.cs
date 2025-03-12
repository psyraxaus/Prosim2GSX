using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Prosim2GSX.Services;

namespace Prosim2GSX.UI.EFB.Services
{
    /// <summary>
    /// Dummy implementation of IProsimDoorService for use in the EFB UI when real services are not available
    /// </summary>
    public class DummyProsimDoorService : IProsimDoorService, IDisposable
    {
        public event EventHandler<DoorStateChangedEventArgs> DoorStateChanged;

        public bool IsForwardLeftDoorOpen() => false;
        public bool IsForwardRightDoorOpen() => false;
        public bool IsAftLeftDoorOpen() => false;
        public bool IsAftRightDoorOpen() => false;
        public bool IsForwardCargoDoorOpen() => false;
        public bool IsAftCargoDoorOpen() => false;

        public bool ToggleForwardLeftDoor() => false;
        public bool ToggleForwardRightDoor() => false;
        public bool ToggleAftLeftDoor() => false;
        public bool ToggleAftRightDoor() => false;
        public bool ToggleForwardCargoDoor() => false;
        public bool ToggleAftCargoDoor() => false;

        public void SetForwardLeftDoor(bool open) { }
        public void SetForwardRightDoor(bool open) { }
        public void SetAftLeftDoor(bool open) { }
        public void SetAftRightDoor(bool open) { }
        public void SetForwardCargoDoor(bool open) { }
        public void SetAftCargoDoor(bool open) { }

        public void InitializeDoorStates() { }

        public void Dispose() { }
    }

    /// <summary>
    /// Dummy implementation of IProsimEquipmentService for use in the EFB UI when real services are not available
    /// </summary>
    public class DummyProsimEquipmentService : IProsimEquipmentService, IDisposable
    {
        public event EventHandler<EquipmentStateChangedEventArgs> EquipmentStateChanged;

        public bool IsJetwayConnected() => false;
        public bool IsStairsConnected() => false;
        public bool IsGpuConnected() => false;
        public bool IsPcaConnected() => false;
        public bool AreChocksPlaced() => false;

        public void ToggleJetway() { }
        public void ToggleStairs() { }
        public void ToggleGpu() { }
        public void TogglePca() { }
        public void ToggleChocks() { }

        public void ConnectJetway() { }
        public void ConnectStairs() { }
        public void ConnectGpu() { }
        public void ConnectPca() { }
        public void PlaceChocks() { }

        public void DisconnectJetway() { }
        public void DisconnectStairs() { }
        public void DisconnectGpu() { }
        public void DisconnectPca() { }
        public void RemoveChocks() { }

        public void SetServicePCA(bool connected) { }
        public void SetServiceChocks(bool placed) { }
        public void SetServiceGPU(bool connected) { }

        public void Dispose() { }
    }

    /// <summary>
    /// Dummy implementation of IGSXFuelCoordinator for use in the EFB UI when real services are not available
    /// </summary>
    public class DummyGSXFuelCoordinator : IGSXFuelCoordinator, IDisposable
    {
        public event EventHandler<FuelStateChangedEventArgs> FuelStateChanged;
        public event EventHandler<RefuelingProgressChangedEventArgs> RefuelingProgressChanged;

        public bool IsRefuelingInProgress => false;
        public bool IsDefuelingInProgress => false;
        public Prosim2GSX.Services.RefuelingState RefuelingState => Prosim2GSX.Services.RefuelingState.Idle;
        public double FuelPlanned => 0;
        public double FuelCurrent => 0;
        public string FuelUnits => "KGS";
        public int RefuelingProgressPercentage => 0;
        public float FuelRateKGS => 28.0f;

        public void Initialize() { }
        public void RegisterForStateChanges(IGSXStateManager stateManager) { }
        public void SetServiceOrchestrator(IGSXServiceOrchestrator serviceOrchestrator) { }
        public void SetEventAggregator(IEventAggregator eventAggregator) { }
        
        public int GetRefuelingProgress() => 0;
        public double GetRefuelingProgress(bool asDouble) => 0;
        
        public bool StartRefueling() => false;
        public bool StopRefueling() => false;
        public bool StartDefueling() => false;
        public bool StopDefueling() => false;
        public bool UpdateFuelAmount(double amount) => false;
        
        public Task<bool> StartRefuelingAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> StopRefuelingAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> StartDefuelingAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> StopDefuelingAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> UpdateFuelAmountAsync(double amount, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> RequestRefuelingAsync(double targetFuelAmount, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> CancelRefuelingAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task SynchronizeFuelQuantitiesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<double> CalculateRequiredFuelAsync(CancellationToken cancellationToken = default) => Task.FromResult(0.0);
        public Task ManageFuelForStateAsync(FlightState state, CancellationToken cancellationToken = default) => Task.CompletedTask;
        
        public bool RequestRefueling(double targetFuelAmount) => false;
        public bool CancelRefueling() => false;
        
        public void Dispose() { }
    }

    /// <summary>
    /// Dummy implementation of IGSXServiceOrchestrator for use in the EFB UI when real services are not available
    /// </summary>
    public class DummyGSXServiceOrchestrator : IGSXServiceOrchestrator, IGSXServiceCoordinator, IDisposable
    {
        public event EventHandler<ServiceStatusChangedEventArgs> ServiceStatusChanged;
        public event EventHandler<PredictedStateChangedEventArgs> PredictedStateChanged;

        public int Interval => 1000;
        public bool IsBoardingInProgress => false;
        public bool IsDeBoardingInProgress => false;
        public bool IsRefuelingInProgress => false;
        public bool IsCateringInProgress => false;
        public bool IsCargoLoadingInProgress => false;
        public bool IsCargoUnloadingInProgress => false;
        public event EventHandler<ServicePredictionEventArgs> ServicePredicted;

        public void Initialize() { }
        public IGSXServiceCoordinator GetCoordinator() => this;
        
        public bool RequestBoarding() => false;
        public bool RequestDeBoarding() => false;
        public bool RequestRefueling(double targetFuelAmount) => false;
        public bool RequestCatering() => false;
        public bool RequestCargoLoading() => false;
        public bool RequestCargoUnloading() => false;
        
        public bool CancelBoarding() => false;
        public bool CancelDeBoarding() => false;
        public bool CancelRefueling() => false;
        public bool CancelCatering() => false;
        public bool CancelCargoLoading() => false;
        public bool CancelCargoUnloading() => false;
        
        public void OrchestrateServices(FlightState state, AircraftParameters parameters) { }
        public IReadOnlyCollection<ServicePrediction> PredictServices(FlightState state, AircraftParameters parameters) => 
            new List<ServicePrediction>().AsReadOnly();
        
        public void RegisterPreServiceCallback(string serviceType, Action<ServiceEventArgs> callback) { }
        public void RegisterPostServiceCallback(string serviceType, Action<ServiceEventArgs> callback) { }
        public void UnregisterPreServiceCallback(string serviceType, Action<ServiceEventArgs> callback) { }
        public void UnregisterPostServiceCallback(string serviceType, Action<ServiceEventArgs> callback) { }
        
        // IGSXServiceCoordinator implementation
        public void RunLoadingServices(int refuelState, int cateringState) { }
        public void RunDepartureServices(int departureState) { }
        public void RunArrivalServices(int deboardState) { }
        public void RunDeboardingService(int deboardState) { }
        
        public bool IsRefuelingComplete() => false;
        public bool IsBoardingComplete() => false;
        public bool IsCateringComplete() => false;
        public bool IsFinalLoadsheetSent() => false;
        public bool IsPreliminaryLoadsheetSent() => false;
        public bool IsEquipmentRemoved() => false;
        public bool IsPushbackComplete() => false;
        public bool IsDeboardingComplete() => false;
        
        public void SetPassengers(int pax) { }
        public void CallJetwayStairs() { }
        public void ResetServiceStatus() { }
        
        public void Dispose() { }
    }
}
