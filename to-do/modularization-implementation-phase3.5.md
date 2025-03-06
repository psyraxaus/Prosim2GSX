# Phase 3.5: GSXLoadsheetManager Implementation

## Overview

This document outlines the implementation plan for Phase 3.5 of the Prosim2GSX modularization strategy. In this phase, we'll extract loadsheet management functionality from the GsxController into a dedicated service.

## Implementation Steps

### 1. Create LoadsheetGeneratedEventArgs.cs

Create a new event args class in the Services folder:

```csharp
using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Event arguments for loadsheet generation
    /// </summary>
    public class LoadsheetGeneratedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the loadsheet type
        /// </summary>
        public LoadsheetType Type { get; }
        
        /// <summary>
        /// Gets the formatted loadsheet
        /// </summary>
        public string FormattedLoadsheet { get; }
        
        /// <summary>
        /// Gets the timestamp of the loadsheet generation
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// Initializes a new instance of the LoadsheetGeneratedEventArgs class
        /// </summary>
        /// <param name="type">The loadsheet type</param>
        /// <param name="formattedLoadsheet">The formatted loadsheet</param>
        public LoadsheetGeneratedEventArgs(LoadsheetType type, string formattedLoadsheet)
        {
            Type = type;
            FormattedLoadsheet = formattedLoadsheet;
            Timestamp = DateTime.Now;
        }
    }
    
    /// <summary>
    /// Loadsheet types
    /// </summary>
    public enum LoadsheetType
    {
        Preliminary,
        Final
    }
}
```

### 2. Create IGSXLoadsheetManager.cs

Create a new interface file in the Services folder:

```csharp
using System;
using System.Collections.Generic;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for GSX loadsheet management
    /// </summary>
    public interface IGSXLoadsheetManager
    {
        /// <summary>
        /// Event raised when a loadsheet is generated
        /// </summary>
        event EventHandler<LoadsheetGeneratedEventArgs> LoadsheetGenerated;
        
        /// <summary>
        /// Gets whether the preliminary loadsheet has been sent
        /// </summary>
        bool IsPreliminaryLoadsheetSent { get; }
        
        /// <summary>
        /// Gets whether the final loadsheet has been sent
        /// </summary>
        bool IsFinalLoadsheetSent { get; }
        
        /// <summary>
        /// Initializes the loadsheet manager
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Formats a loadsheet
        /// </summary>
        /// <param name="flightNumber">The flight number</param>
        /// <param name="origin">The origin airport</param>
        /// <param name="destination">The destination airport</param>
        /// <param name="date">The flight date</param>
        /// <param name="time">The flight time</param>
        /// <param name="captain">The captain's name</param>
        /// <param name="pax">The number of passengers</param>
        /// <param name="cargo">The cargo weight</param>
        /// <param name="zfw">The zero fuel weight</param>
        /// <param name="zfwcg">The zero fuel weight center of gravity</param>
        /// <param name="tow">The takeoff weight</param>
        /// <param name="towcg">The takeoff weight center of gravity</param>
        /// <param name="fuel">The fuel weight</param>
        /// <param name="type">The loadsheet type</param>
        /// <returns>The formatted loadsheet</returns>
        string FormatLoadsheet(string flightNumber, string origin, string destination, string date, string time, string captain, int pax, double cargo, double zfw, double zfwcg, double tow, double towcg, double fuel, LoadsheetType type);
        
        /// <summary>
        /// Gets the weight limitation
        /// </summary>
        /// <param name="type">The weight limitation type</param>
        /// <param name="weight">The weight</param>
        /// <returns>The weight limitation</returns>
        string GetWeightLimitation(string type, double weight);
        
        /// <summary>
        /// Gets the loadsheet differences
        /// </summary>
        /// <param name="prelimData">The preliminary data</param>
        /// <param name="finalData">The final data</param>
        /// <returns>The loadsheet differences</returns>
        Dictionary<string, string> GetLoadsheetDifferences((double zfw, double tow, int pax, double maczfw, double mactow, double fuel) prelimData, (double zfw, double tow, int pax, double maczfw, double mactow, double fuel) finalData);
        
        /// <summary>
        /// Gets a random name
        /// </summary>
        /// <returns>A random name</returns>
        string GetRandomName();
        
        /// <summary>
        /// Gets a random license number
        /// </summary>
        /// <returns>A random license number</returns>
        string GetRandomLicenseNumber();
        
        /// <summary>
        /// Sends a preliminary loadsheet
        /// </summary>
        /// <param name="flightNumber">The flight number</param>
        /// <param name="loadedData">The loaded data</param>
        /// <returns>True if the loadsheet was sent successfully, false otherwise</returns>
        bool SendPreliminaryLoadsheet(string flightNumber, (double zfw, double tow, int pax, double maczfw, double mactow, double fuel) loadedData);
        
        /// <summary>
        /// Sends a final loadsheet
        /// </summary>
        /// <param name="flightNumber">The flight number</param>
        /// <param name="loadedData">The loaded data</param>
        /// <param name="prelimData">The preliminary data</param>
        /// <returns>True if the loadsheet was sent successfully, false otherwise</returns>
        bool SendFinalLoadsheet(string flightNumber, (double zfw, double tow, int pax, double maczfw, double mactow, double fuel) loadedData, (double zfw, double tow, int pax, double maczfw, double mactow, double fuel) prelimData);
    }
}
```

### 3. Create GSXLoadsheetManager.cs

Create a new implementation file in the Services folder:

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Service for GSX loadsheet management
    /// </summary>
    public class GSXLoadsheetManager : IGSXLoadsheetManager
    {
        private readonly IAcarsService acarsService;
        private readonly FlightPlan flightPlan;
        private readonly ServiceModel model;
        
        private bool preliminaryLoadsheetSent = false;
        private bool finalLoadsheetSent = false;
        
        private readonly string[] firstNames = { "John", "James", "Robert", "Michael", "William", "David", "Richard", "Joseph", "Thomas", "Charles", "Christopher", "Daniel", "Matthew", "Anthony", "Mark", "Donald", "Steven", "Paul", "Andrew", "Joshua", "Kenneth", "Kevin", "Brian", "George", "Timothy", "Ronald", "Edward", "Jason", "Jeffrey", "Ryan", "Jacob", "Gary", "Nicholas", "Eric", "Jonathan", "Stephen", "Larry", "Justin", "Scott", "Brandon", "Benjamin", "Samuel", "Gregory", "Alexander", "Frank", "Patrick", "Raymond", "Jack", "Dennis", "Jerry", "Tyler", "Aaron", "Jose", "Adam", "Nathan", "Henry", "Douglas", "Zachary", "Peter", "Kyle", "Ethan", "Walter", "Noah", "Jeremy", "Christian", "Keith", "Roger", "Terry", "Gerald", "Harold", "Sean", "Austin", "Carl", "Arthur", "Lawrence", "Dylan", "Jesse", "Jordan", "Bryan", "Billy", "Joe", "Bruce", "Gabriel", "Logan", "Albert", "Willie", "Alan", "Juan", "Wayne", "Elijah", "Randy", "Roy", "Vincent", "Ralph", "Eugene", "Russell", "Bobby", "Mason", "Philip", "Louis" };
        private readonly string[] lastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson", "Walker", "Young", "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill", "Flores", "Green", "Adams", "Nelson", "Baker", "Hall", "Rivera", "Campbell", "Mitchell", "Carter", "Roberts", "Gomez", "Phillips", "Evans", "Turner", "Diaz", "Parker", "Cruz", "Edwards", "Collins", "Reyes", "Stewart", "Morris", "Morales", "Murphy", "Cook", "Rogers", "Gutierrez", "Ortiz", "Morgan", "Cooper", "Peterson", "Bailey", "Reed", "Kelly", "Howard", "Ramos", "Kim", "Cox", "Ward", "Richardson", "Watson", "Brooks", "Chavez", "Wood", "James", "Bennett", "Gray", "Mendoza", "Ruiz", "Hughes", "Price", "Alvarez", "Castillo", "Sanders", "Patel", "Myers", "Long", "Ross", "Foster", "Jimenez" };
        
        /// <summary>
        /// Event raised when a loadsheet is generated
        /// </summary>
        public event EventHandler<LoadsheetGeneratedEventArgs> LoadsheetGenerated;
        
        /// <summary>
        /// Gets whether the preliminary loadsheet has been sent
        /// </summary>
        public bool IsPreliminaryLoadsheetSent => preliminaryLoadsheetSent;
        
        /// <summary>
        /// Gets whether the final loadsheet has been sent
        /// </summary>
        public bool IsFinalLoadsheetSent => finalLoadsheetSent;
        
        /// <summary>
        /// Initializes a new instance of the GSXLoadsheetManager class
        /// </summary>
        public GSXLoadsheetManager(IAcarsService acarsService, FlightPlan flightPlan, ServiceModel model)
        {
            this.acarsService = acarsService;
            this.flightPlan = flightPlan;
            this.model = model;
        }
        
        /// <summary>
        /// Initializes the loadsheet manager
        /// </summary>
        public void Initialize()
        {
            preliminaryLoadsheetSent = false;
            finalLoadsheetSent = false;
            
            Logger.Log(LogLevel.Information, "GSXLoadsheetManager:Initialize", "Loadsheet manager initialized");
        }
        
        /// <summary>
        /// Formats a loadsheet
        /// </summary>
        public string FormatLoadsheet(string flightNumber, string origin, string destination, string date, string time, string captain, int pax, double cargo, double zfw, double zfwcg, double tow, double towcg, double fuel, LoadsheetType type)
        {
            string loadsheet = "";
            string typeString = type == LoadsheetType.Preliminary ? "PRELIMINARY" : "FINAL";
            
            loadsheet += $"LOADSHEET {typeString}\n";
            loadsheet += $"FLIGHT: {flightNumber}\n";
            loadsheet += $"FROM: {origin} TO: {destination}\n";
            loadsheet += $"DATE: {date} TIME: {time}\n";
            loadsheet += $"CAPTAIN: {captain}\n";
            loadsheet += $"LICENSE: {GetRandomLicenseNumber()}\n";
            loadsheet += $"PAX: {pax}\n";
            loadsheet += $"CARGO: {cargo:F0} KG\n";
            loadsheet += $"ZFW: {zfw:F0} KG {GetWeightLimitation("ZFW", zfw)}\n";
            loadsheet += $"ZFWCG: {zfwcg:F1} %\n";
            loadsheet += $"TOW: {tow:F0} KG {GetWeightLimitation("TOW", tow)}\n";
            loadsheet += $"TOWCG: {towcg:F1} %\n";
            loadsheet += $"FUEL: {fuel:F0} KG\n";
            
            OnLoadsheetGenerated(type, loadsheet);
            
            return loadsheet;
        }
        
        /// <summary>
        /// Gets the weight limitation
        /// </summary>
        public string GetWeightLimitation(string type, double weight)
        {
            double limit = 0;
            
            switch (type)
            {
                case "ZFW":
                    limit = 61000;
                    break;
                case "TOW":
                    limit = 77000;
                    break;
                default:
                    return "";
            }
            
            double percentage = weight / limit * 100;
            
            if (percentage > 100)
                return $"OVERWEIGHT ({percentage:F0}%)";
            else if (percentage > 95)
                return $"NEAR LIMIT ({percentage:F0}%)";
            else
                return $"OK ({percentage:F0}%)";
        }
        
        /// <summary>
        /// Gets the loadsheet differences
        /// </summary>
        public Dictionary<string, string> GetLoadsheetDifferences((double zfw, double tow, int pax, double maczfw, double mactow, double fuel) prelimData, (double zfw, double tow, int pax, double maczfw, double mactow, double fuel) finalData)
        {
            var differences = new Dictionary<string, string>();
            
            if (Math.Abs(prelimData.zfw - finalData.zfw) > 10)
                differences.Add("ZFW", $"{prelimData.zfw:F0} -> {finalData.zfw:F0} ({finalData.zfw - prelimData.zfw:+0;-#})");
            
            if (Math.Abs(prelimData.tow - finalData.tow) > 10)
                differences.Add("TOW", $"{prelimData.tow:F0} -> {finalData.tow:F0} ({finalData.tow - prelimData.tow:+0;-#})");
            
            if (prelimData.pax != finalData.pax)
                differences.Add("PAX", $"{prelimData.pax} -> {finalData.pax} ({finalData.pax - prelimData.pax:+0;-#})");
            
            if (Math.Abs(prelimData.maczfw - finalData.maczfw) > 0.1)
                differences.Add("MACZFW", $"{prelimData.maczfw:F1} -> {finalData.maczfw:F1} ({finalData.maczfw - prelimData.maczfw:+0.0;-0.0})");
            
            if (Math.Abs(prelimData.mactow - finalData.mactow) > 0.1)
                differences.Add("MACTOW", $"{prelimData.mactow:F1} -> {finalData.mactow:F1} ({finalData.mactow - prelimData.mactow:+0.0;-0.0})");
            
            if (Math.Abs(prelimData.fuel - finalData.fuel) > 10)
                differences.Add("FUEL", $"{prelimData.fuel:F0} -> {finalData.fuel:F0} ({finalData.fuel - prelimData.fuel:+0;-#})");
            
            return differences;
        }
        
        /// <summary>
        /// Gets a random name
        /// </summary>
        public string GetRandomName()
        {
            var random = new Random();
            string firstName = firstNames[random.Next(firstNames.Length)];
            string lastName = lastNames[random.Next(lastNames.Length)];
            
            return $"{firstName} {lastName}";
        }
        
        /// <summary>
        /// Gets a random license number
        /// </summary>
        public string GetRandomLicenseNumber()
        {
            var random = new Random();
            string licenseNumber = "";
            
            for (int i = 0; i < 6; i++)
            {
                licenseNumber += random.Next(10).ToString();
            }
            
            return licenseNumber;
        }
        
        /// <summary>
        /// Sends a preliminary loadsheet
        /// </summary>
        public bool SendPreliminaryLoadsheet(string flightNumber, (double zfw, double tow, int pax, double maczfw, double mactow, double fuel) loadedData)
        {
            if (!model.UseAcars || string.IsNullOrEmpty(flightNumber))
                return false;
            
            try
            {
                string origin = flightPlan.Origin;
                string destination = flightPlan.Destination;
                string date = DateTime.Now.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture).ToUpper();
                string time = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
                string captain = GetRandomName();
                
                string loadsheet = FormatLoadsheet(
                    flightNumber,
                    origin,
                    destination,
                    date,
                    time,
                    captain,
                    loadedData.pax,
                    0, // Cargo
                    loadedData.zfw,
                    loadedData.maczfw,
                    loadedData.tow,
                    loadedData.mactow,
                    loadedData.fuel,
                    LoadsheetType.Preliminary
                );
                
                acarsService.SendPreliminaryLoadsheetAsync(flightNumber, loadedData);
                preliminaryLoadsheetSent = true;
                
                Logger.Log(LogLevel.Information, "GSXLoadsheetManager:SendPreliminaryLoadsheet", "Preliminary loadsheet sent");
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXLoadsheetManager:SendPreliminaryLoadsheet", $"Error sending preliminary loadsheet: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Sends a final loadsheet
        /// </summary>
        public bool SendFinalLoadsheet(string flightNumber, (double zfw, double tow, int pax, double maczfw, double mactow, double fuel) loadedData, (double zfw, double tow, int pax, double maczfw, double mactow, double fuel) prelimData)
        {
            if (!model.UseAcars || string.IsNullOrEmpty(flightNumber))
                return false;
            
            try
            {
                string origin = flightPlan.Origin;
                string destination = flightPlan.Destination;
                string date = DateTime.Now.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture).ToUpper();
                string time = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
                string captain = GetRandomName();
                
                string loadsheet = FormatLoadsheet(
                    flightNumber,
                    origin,
                    destination,
                    date,
                    time,
                    captain,
                    loadedData.pax,
                    0, // Cargo
                    loadedData.zfw,
                    loadedData.maczfw,
                    loadedData.tow,
                    loadedData.mactow,
                    loadedData.fuel,
                    LoadsheetType.Final
                );
                
                acarsService.SendFinalLoadsheetAsync(flightNumber, loadedData, prelimData);
                finalLoadsheetSent = true;
                
                Logger.Log(LogLevel.Information, "GSXLoadsheetManager:SendFinalLoadsheet", "Final loadsheet sent");
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "GSXLoadsheetManager:SendFinalLoadsheet", $"Error sending final loadsheet: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Raises the LoadsheetGenerated event
        /// </summary>
        protected virtual void OnLoadsheetGenerated(LoadsheetType type, string formattedLoadsheet)
        {
            LoadsheetGenerated?.Invoke(this, new LoadsheetGeneratedEventArgs(type, formattedLoadsheet));
        }
    }
}
```

### 4. Update GsxController.cs

Update the GsxController class to use the new service:

```csharp
// Add new field
private readonly IGSXLoadsheetManager loadsheetManager;

// Update constructor
public GsxController(ServiceModel model, ProsimController prosimController, FlightPlan flightPlan, IAcarsService acarsService, IGSXMenuService menuService, IGSXAudioService audioService, IGSXStateManager stateManager, IGSXServiceCoordinator serviceCoordinator, IGSXDoorManager doorManager, IGSXEquipmentManager equipmentManager, IGSXLoadsheetManager loadsheetManager)
{
    Model = model;
    ProsimController = prosimController;
    FlightPlan = flightPlan;
    this.acarsService = acarsService;
    this.menuService = menuService;
    this.audioService = audioService;
    this.stateManager = stateManager;
    this.serviceCoordinator = serviceCoordinator;
    this.doorManager = doorManager;
    this.equipmentManager = equipmentManager;
    this.loadsheetManager = loadsheetManager;

    SimConnect = IPCManager.SimConnect;
    // Subscribe to SimConnect variables...
    
    // Initialize services
    stateManager.Initialize();
    serviceCoordinator.Initialize();
    doorManager.Initialize();
    equipmentManager.Initialize();
    loadsheetManager.Initialize();
    
    // Subscribe to events
    stateManager.StateChanged += OnStateChanged;
    serviceCoordinator.ServiceOperationStatusChanged += OnServiceOperationStatusChanged;
    doorManager.DoorStateChanged += OnDoorStateChanged;
    equipmentManager.EquipmentStateChanged += OnEquipmentStateChanged;
    loadsheetManager.LoadsheetGenerated += OnLoadsheetGenerated;
    
    if (Model.TestArrival)
        ProsimController.Update(true);
}

// Add event handler for loadsheet generation
private void OnLoadsheetGenerated(object sender, LoadsheetGeneratedEventArgs e)
{
    // Handle loadsheet generation
    Logger.Log(LogLevel.Information, "GsxController:OnLoadsheetGenerated", $"{e.Type} loadsheet generated");
}

// Update RunServices method to use loadsheetManager
public void RunServices()
{
    // ... existing code ...
    
    // Handle DEPARTURE state
    if (stateManager.IsDeparture())
    {
        // Get sim Zulu Time and send Prelim Loadsheet
        if (!loadsheetManager.IsPreliminaryLoadsheetSent)
        {
            var simTime = SimConnect.ReadEnvVar("ZULU TIME", "Seconds");
            TimeSpan time = TimeSpan.FromSeconds(simTime);
            Logger.Log(LogLevel.Debug, "GsxController:RunServices", $"ZULU time - {simTime}");

            string flightNumber = ProsimController.GetFMSFlightNumber();

            if (Model.UseAcars && !string.IsNullOrEmpty(flightNumber))
            {
                var prelimLoadedData = ProsimController.GetLoadedData("prelim");
                loadsheetManager.SendPreliminaryLoadsheet(flightNumber, prelimLoadedData);
            }
        }

        // ... existing code ...
    }
    
    // Handle DEPARTURE services
    private void RunDEPARTUREServices()
    {
        // ... existing code ...
        
        // LOADSHEET
        if (!loadsheetManager.IsFinalLoadsheetSent)
        {
            if (delay == 0)
            {
                delay = new Random().Next(90, 150);
                delayCounter = 0;
                Logger.Log(LogLevel.Information, "GsxController:RunDEPARTUREServices", $"Final Loadsheet in {delay}s");
            }

            if (delayCounter < delay)
            {
                delayCounter++;
                return;
            }
            else
            {
                Logger.Log(LogLevel.Information, "GsxController:RunDEPARTUREServices", $"Transmitting Final Loadsheet ...");
                ProsimController.TriggerFinal();
                
                if (Model.UseAcars)
                {
                    var finalLoadedData = ProsimController.GetLoadedData("final");
                    var prelimData = (prelimZfw, prelimTow, prelimPax, prelimMacZfw, prelimMacTow, prelimFuel);
                    loadsheetManager.SendFinalLoadsheet(ProsimController.GetFMSFlightNumber(), finalLoadedData, prelimData);
                }
            }
        }
        
        // ... existing code ...
    }
    
    // ... existing code ...
}

// Clean up resources
public void Dispose()
{
    // Unsubscribe from events
    if (stateManager != null)
    {
        stateManager.StateChanged -= OnStateChanged;
    }
    
    if (serviceCoordinator != null)
    {
        serviceCoordinator.ServiceOperationStatusChanged -= OnServiceOperationStatusChanged;
    }
    
    if (doorManager != null)
    {
        doorManager.DoorStateChanged -= OnDoorStateChanged;
    }
    
    if (equipmentManager != null)
    {
        equipmentManager.EquipmentStateChanged -= OnEquipmentStateChanged;
    }
    
    if (loadsheetManager != null)
    {
        loadsheetManager.LoadsheetGenerated -= OnLoadsheetGenerated;
    }
    
    // ... other cleanup code ...
}
```

### 5. Update ServiceController.cs

Update the ServiceController class to initialize the new service:

```csharp
protected void InitializeServices()
{
    Logger.Log(LogLevel.Information, "ServiceController:InitializeServices", "Initializing services...");
    
    // Step 1: Create FlightPlanService
    var flightPlanService = new FlightPlanService(Model);
    
    // Step 2: Create FlightPlan
    FlightPlan = new FlightPlan(Model, flightPlanService);
    
    // Step 3: Load flight plan
    if (!FlightPlan.Load())
    {
        Logger.Log(LogLevel.Warning, "ServiceController:InitializeServices", "Could not load flight plan, will retry in service loop");
    }
    
    // Step 4: Initialize FlightPlan in ProsimController
    ProsimController.InitializeFlightPlan(FlightPlan);
    
    // Step 5: Create AcarsService
    var acarsService = new AcarsService(Model.AcarsSecret, Model.AcarsNetworkUrl);
    
    // Step 6: Create GSX services
    var menuService = new GSXMenuService(Model, IPCManager.SimConnect);
    var audioService = new GSXAudioService(Model, IPCManager.SimConnect);
    var stateManager = new GSXStateManager();
    var serviceCoordinator = new GSXServiceCoordinator(Model, IPCManager.SimConnect, ProsimController, acarsService, menuService);
    var doorManager = new GSXDoorManager(ProsimController);
    var equipmentManager = new GSXEquipmentManager(ProsimController, IPCManager.SimConnect);
    var loadsheetManager = new GSXLoadsheetManager(acarsService, FlightPlan, Model);
    
    // Step 7: Create GsxController
    var gsxController = new GsxController(Model, ProsimController, FlightPlan, acarsService, menuService, audioService, stateManager, serviceCoordinator, doorManager, equipmentManager, loadsheetManager);
    
    // Store the GsxController in IPCManager
    IPCManager.GsxController = gsxController;
    
    Logger.Log(LogLevel.Information, "ServiceController:InitializeServices", "Services initialized successfully");
}
```

### 6. Add Unit Tests

Create unit tests for the new service in the Tests folder:

```csharp
[TestClass]
public class GSXLoadsheetManagerTests
{
    [TestMethod]
    public void Initialize_ResetsLoadsheetStates()
    {
        // Arrange
        var acarsServiceMock = new Mock<IAcarsService>();
        var flightPlanMock = new Mock<FlightPlan>(new ServiceModel(), new FlightPlanService(new ServiceModel()));
        var model = new ServiceModel();
        var loadsheetManager = new GSXLoadsheetManager(acarsServiceMock.Object, flightPlanMock.Object, model);
        
        // Act
        loadsheetManager.Initialize();
        
        // Assert
        Assert.IsFalse(loadsheetManager.IsPreliminaryLoadsheetSent);
        Assert.IsFalse(loadsheetManager.IsFinalLoadsheetSent);
    }
    
    [TestMethod]
    public void FormatLoadsheet_RaisesEvent()
    {
        // Arrange
        var acarsServiceMock = new Mock<IAcarsService>();
        var flightPlanMock = new Mock<FlightPlan>(new ServiceModel(), new FlightPlanService(new ServiceModel()));
        var model = new ServiceModel();
        var loadsheetManager = new GSXLoadsheetManager(acarsServiceMock.Object, flightPlanMock.Object, model);
        
        bool eventRaised = false;
        LoadsheetGeneratedEventArgs eventArgs = null;
        loadsheetManager.LoadsheetGenerated += (sender, e) => 
        {
            eventRaised = true;
            eventArgs = e;
        };
        
        // Act
        string loadsheet = loadsheetManager.FormatLoadsheet(
            "ABC123",
            "KJFK",
            "KLAX",
            "01-JAN-2023",
            "12:00",
            "John Doe",
            150,
            1000,
            50000,
            25.5,
            60000,
            26.5,
            10000,
            LoadsheetType.Preliminary
        );
        
        // Assert
        Assert.IsTrue(eventRaised);
        Assert.AreEqual(LoadsheetType.Preliminary, eventArgs.Type);
        Assert.IsNotNull(eventArgs.FormattedLoadsheet);
        Assert.IsTrue(loadsheet.Contains("LOADSHEET PRELIMINARY"));
        Assert.IsTrue(loadsheet.Contains("FLIGHT: ABC123"));
        Assert.IsTrue(loadsheet.Contains("FROM: KJFK TO: KLAX"));
        Assert.IsTrue(loadsheet.Contains("PAX: 150"));
    }
    
    [TestMethod]
    public void GetWeightLimitation_ReturnsCorrectLimitation()
    {
        // Arrange
        var acarsServiceMock = new Mock<IAcarsService>();
        var flightPlanMock = new Mock<FlightPlan>(new ServiceModel(), new FlightPlanService(new ServiceModel()));
        var model = new ServiceModel();
        var loadsheetManager = new GSXLoadsheetManager(acarsServiceMock.Object, flightPlanMock.Object, model);
        
        // Act
        string zfwLimitation = loadsheetManager.GetWeightLimitation("ZFW", 60000);
        string towLimitation = loadsheetManager.GetWeightLimitation("TOW", 80000);
        
        // Assert
        Assert.IsTrue(zfwLimitation.Contains("NEAR LIMIT"));
        Assert.IsTrue(towLimitation.Contains("OVERWEIGHT"));
    }
    
    [TestMethod]
    public void GetLoadsheetDifferences_ReturnsCorrectDifferences()
    {
        // Arrange
        var acarsServiceMock = new Mock<IAcarsService>();
        var flightPlanMock = new Mock<FlightPlan>(new ServiceModel(), new FlightPlanService(new ServiceModel()));
        var model = new ServiceModel();
        var loadsheetManager = new GSXLoadsheetManager(acarsServiceMock.Object, flightPlanMock.Object, model);
        
        var prelimData = (zfw: 50000.0, tow: 60000.0, pax: 150, maczfw: 25.0, mactow: 26.0, fuel: 10000.0);
        var finalData = (zfw: 51000.0, tow: 62000.0, pax: 155, maczfw: 25.5, mactow: 26.5, fuel: 11000.0);
        
        // Act
        var differences = loadsheetManager.GetLoadsheetDifferences(prelimData, finalData);
        
        // Assert
        Assert.AreEqual(6, differences.Count);
        Assert.IsTrue(differences.ContainsKey("ZFW"));
        Assert.IsTrue(differences.ContainsKey("TOW"));
        Assert.IsTrue(differences.ContainsKey("PAX"));
        Assert.IsTrue(differences.ContainsKey("MACZFW"));
        Assert.IsTrue(differences.ContainsKey("MACTOW"));
        Assert.IsTrue(differences.ContainsKey("FUEL"));
        Assert.IsTrue(differences["ZFW"].Contains("+1000"));
        Assert.IsTrue(differences["PAX"].Contains("+5"));
    }
    
    [TestMethod]
    public void GetRandomName_ReturnsValidName()
    {
        // Arrange
        var acarsServiceMock = new Mock<IAcarsService>();
        var flightPlanMock = new Mock<FlightPlan>(new ServiceModel(), new FlightPlanService(new ServiceModel()));
        var model = new ServiceModel();
        var loadsheetManager = new GSXLoadsheetManager(acarsServiceMock.Object, flightPlanMock.Object, model);
        
        // Act
        string name = loadsheetManager.GetRandomName();
        
        // Assert
        Assert.IsNotNull(name);
        Assert.IsTrue(name.Contains(" ")); // First and last name should be separated by a space
        Assert.AreEqual(2, name.Split(' ').Length); // Should have exactly two parts
    }
    
    [TestMethod]
    public void GetRandomLicenseNumber_ReturnsValidLicenseNumber()
    {
        // Arrange
        var acarsServiceMock = new Mock<IAcarsService>();
        var flightPlanMock = new Mock<FlightPlan>(new ServiceModel(), new FlightPlanService(new ServiceModel()));
        var model = new ServiceModel();
        var loadsheetManager = new GSXLoadsheetManager(acarsServiceMock.Object, flightPlanMock.Object, model);
        
        // Act
        string licenseNumber = loadsheetManager.GetRandomLicenseNumber();
        
        // Assert
        Assert.IsNotNull(licenseNumber);
        Assert.AreEqual(6, licenseNumber.Length); // Should be 6 digits
        Assert.IsTrue(int.TryParse(licenseNumber, out _)); // Should be a valid integer
    }
    
    [TestMethod]
    public void SendPreliminaryLoadsheet_SetsFlag()
    {
        // Arrange
        var acarsServiceMock = new Mock<IAcarsService>();
        var flightPlanMock = new Mock<FlightPlan>(new ServiceModel(), new FlightPlanService(new ServiceModel()));
        flightPlanMock.Setup(f => f.Origin).Returns("KJFK");
        flightPlanMock.Setup(f => f.Destination).Returns("KLAX");
        
        var model = new ServiceModel { UseAcars = true };
        var loadsheetManager = new GSXLoadsheetManager(acarsServiceMock.Object, flightPlanMock.Object, model);
        
        var loadedData = (zfw: 50000.0, tow: 60000.0, pax: 150, maczfw: 25.0, mactow: 26.0, fuel: 10000.0);
        
        // Act
        bool result = loadsheetManager.SendPreliminaryLoadsheet("ABC123", loadedData);
        
        // Assert
        Assert.IsTrue(result);
        Assert.IsTrue(loadsheetManager.IsPreliminaryLoadsheetSent);
        acarsServiceMock.Verify(a => a.SendPreliminaryLoadsheetAsync(It.IsAny<string>(), It.IsAny<(double, double, int, double, double, double)>()), Times.Once);
    }
    
    [TestMethod]
    public void SendFinalLoadsheet_SetsFlag()
    {
        // Arrange
        var acarsServiceMock = new Mock<IAcarsService>();
        var flightPlanMock = new Mock<FlightPlan>(new ServiceModel(), new FlightPlanService(new ServiceModel()));
        flightPlanMock.Setup(f => f.Origin).Returns("KJFK");
        flightPlanMock.Setup(f => f.Destination).Returns("KLAX");
        
        var model = new ServiceModel { UseAcars = true };
        var loadsheetManager = new GSXLoadsheetManager(acarsServiceMock.Object, flightPlanMock.Object, model);
        
        var loadedData = (zfw: 51000.0, tow: 62000.0, pax: 155, maczfw: 25.5, mactow: 26.5, fuel: 11000.0);
        var prelimData = (zfw: 50000.0, tow: 60000.0, pax: 150, maczfw: 25.0, mactow: 26.0, fuel: 10000.0);
        
        // Act
        bool result = loadsheetManager.SendFinalLoadsheet("ABC123", loadedData, prelimData);
        
        // Assert
        Assert.IsTrue(result);
        Assert.IsTrue(loadsheetManager.IsFinalLoadsheetSent);
        acarsServiceMock.Verify(a => a.SendFinalLoadsheetAsync(It.IsAny<string>(), It.IsAny<(double, double, int, double, double, double)>(), It.IsAny<(double, double, int, double, double, double)>()), Times.Once);
    }
}
```

### 7. Test the Implementation

Test the implementation to ensure it works correctly.

## Benefits

1. **Improved Separation of Concerns**
   - Loadsheet management is now handled by a dedicated service
   - The service has a single responsibility
   - GsxController is simplified and more focused

2. **Enhanced Testability**
   - Loadsheet operations can be tested in isolation
   - Dependencies are explicit and can be mocked
   - Unit tests can be written for each loadsheet operation

3. **Better Maintainability**
   - Changes to loadsheet management can be made without affecting other parts of the system
   - Code is more organized and easier to understand
   - New loadsheet operations can be added without modifying GsxController

4. **Event-Based Communication**
   - Components can subscribe to loadsheet generation events
   - Reduces tight coupling between components
   - Makes the system more extensible

## Next Steps

After implementing Phase 3.5, we'll proceed with Phase 3.6 to refine the GsxController into a thin facade that delegates to the appropriate services.
