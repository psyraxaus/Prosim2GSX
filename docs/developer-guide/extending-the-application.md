# Extending Prosim2GSX

This guide provides instructions for extending the Prosim2GSX application with new features and functionality.

## Table of Contents

1. [Adding a New Service](#adding-a-new-service)
2. [Modifying Existing Services](#modifying-existing-services)
3. [Adding New Features](#adding-new-features)
4. [Extending the State Machine](#extending-the-state-machine)
5. [Adding New Coordinators](#adding-new-coordinators)

## Adding a New Service

### Overview

Adding a new service to Prosim2GSX involves creating a new interface and implementation, and then registering the service with the ServiceController.

### Steps

1. **Define the Service Interface**

   Create a new interface in the appropriate namespace. Follow the naming convention `IXxxService`.

   ```csharp
   using System;
   using System.Threading.Tasks;

   namespace Prosim2GSX.Services
   {
       public interface IWeatherService
       {
           Task<WeatherData> GetCurrentWeatherAsync(string airport);
           Task<WeatherData> GetForecastAsync(string airport, DateTime time);
           event EventHandler<WeatherChangedEventArgs> WeatherChanged;
       }
   }
   ```

2. **Create Event Arguments**

   If your service raises events, create appropriate event argument classes.

   ```csharp
   using System;

   namespace Prosim2GSX.Services
   {
       public class WeatherChangedEventArgs : EventArgs
       {
           public string Airport { get; }
           public WeatherData Weather { get; }

           public WeatherChangedEventArgs(string airport, WeatherData weather)
           {
               Airport = airport;
               Weather = weather;
           }
       }
   }
   ```

3. **Implement the Service**

   Create a class that implements the interface.

   ```csharp
   using System;
   using System.Threading.Tasks;

   namespace Prosim2GSX.Services
   {
       public class WeatherService : IWeatherService
       {
           private readonly ILogger _logger;
           private readonly ISimConnectService _simConnectService;

           public event EventHandler<WeatherChangedEventArgs> WeatherChanged;

           public WeatherService(ISimConnectService simConnectService, ILogger logger)
           {
               _simConnectService = simConnectService ?? throw new ArgumentNullException(nameof(simConnectService));
               _logger = logger ?? throw new ArgumentNullException(nameof(logger));
           }

           public async Task<WeatherData> GetCurrentWeatherAsync(string airport)
           {
               _logger.LogInformation($"Getting current weather for {airport}");

               try
               {
                   // Implementation details...
                   var temperature = await _simConnectService.GetSimVarAsync<double>("AMBIENT TEMPERATURE");
                   var windSpeed = await _simConnectService.GetSimVarAsync<double>("AMBIENT WIND VELOCITY");
                   var windDirection = await _simConnectService.GetSimVarAsync<double>("AMBIENT WIND DIRECTION");
                   var pressure = await _simConnectService.GetSimVarAsync<double>("AMBIENT PRESSURE");
                   var visibility = await _simConnectService.GetSimVarAsync<double>("AMBIENT VISIBILITY");

                   var weather = new WeatherData
                   {
                       Airport = airport,
                       Temperature = temperature,
                       WindSpeed = windSpeed,
                       WindDirection = windDirection,
                       Pressure = pressure,
                       Visibility = visibility,
                       Timestamp = DateTime.UtcNow
                   };

                   OnWeatherChanged(new WeatherChangedEventArgs(airport, weather));

                   return weather;
               }
               catch (Exception ex)
               {
                   _logger.LogError(ex, $"Failed to get current weather for {airport}");
                   throw;
               }
           }

           public async Task<WeatherData> GetForecastAsync(string airport, DateTime time)
           {
               _logger.LogInformation($"Getting weather forecast for {airport} at {time}");

               try
               {
                   // Implementation details...
                   // This is a simplified example
                   var currentWeather = await GetCurrentWeatherAsync(airport);
                   
                   // Apply some forecast algorithm
                   var forecast = new WeatherData
                   {
                       Airport = airport,
                       Temperature = currentWeather.Temperature + (time - DateTime.UtcNow).Hours * 0.5,
                       WindSpeed = currentWeather.WindSpeed * 1.1,
                       WindDirection = (currentWeather.WindDirection + 10) % 360,
                       Pressure = currentWeather.Pressure - 1,
                       Visibility = currentWeather.Visibility * 0.9,
                       Timestamp = time
                   };

                   return forecast;
               }
               catch (Exception ex)
               {
                   _logger.LogError(ex, $"Failed to get weather forecast for {airport} at {time}");
                   throw;
               }
           }

           protected virtual void OnWeatherChanged(WeatherChangedEventArgs e)
           {
               WeatherChanged?.Invoke(this, e);
           }
       }
   }
   ```

4. **Register the Service with ServiceController**

   Modify the ServiceController to create and register the new service.

   ```csharp
   // In ServiceController.cs
   private IWeatherService _weatherService;

   public IWeatherService GetWeatherService()
   {
       if (_weatherService == null)
       {
           _weatherService = new WeatherService(GetSimConnectService(), _logger);
       }
       return _weatherService;
   }
   ```

5. **Update ServiceFactory**

   If you're using the ServiceFactory pattern, update it to include the new service.

   ```csharp
   // In ServiceFactory.cs
   public IWeatherService CreateWeatherService()
   {
       return new WeatherService(CreateSimConnectService(), _logger);
   }
   ```

6. **Use the Service**

   Now you can use the service in other components.

   ```csharp
   // In some other component
   private readonly IWeatherService _weatherService;

   public SomeComponent(IWeatherService weatherService)
   {
       _weatherService = weatherService ?? throw new ArgumentNullException(nameof(weatherService));
       
       // Subscribe to events
       _weatherService.WeatherChanged += OnWeatherChanged;
   }

   private async Task UpdateWeatherAsync()
   {
       var weather = await _weatherService.GetCurrentWeatherAsync("KSFO");
       // Do something with the weather data
   }

   private void OnWeatherChanged(object sender, WeatherChangedEventArgs e)
   {
       // Handle weather change
   }
   ```

### Considerations

- **Dependency Injection**: Inject dependencies through the constructor
- **Error Handling**: Use try-catch blocks and log exceptions
- **Event-Based Communication**: Raise events for state changes
- **Async/Await**: Use async/await for asynchronous operations
- **Cancellation Support**: Add CancellationToken parameters for long-running operations
- **Thread Safety**: Ensure thread safety for shared state
- **Testability**: Design for testability with interfaces and dependency injection

## Modifying Existing Services

### Overview

Modifying existing services involves understanding the service's responsibilities, dependencies, and consumers before making changes.

### Steps

1. **Identify the Service to Modify**

   Locate the service interface and implementation in the codebase.

   ```csharp
   // Example: IProsimFuelService interface
   public interface IProsimFuelService
   {
       Task<double> GetTotalFuelAsync();
       Task<bool> StartRefuelingAsync(double targetFuelKg);
       // Other methods...
   }
   ```

2. **Understand Dependencies and Consumers**

   Identify dependencies of the service and components that consume the service.

   ```csharp
   // Example: ProsimFuelService dependencies
   public class ProsimFuelService : IProsimFuelService
   {
       private readonly IProsimService _prosimService;
       private readonly ILogger _logger;
       
       // Constructor with dependencies
       public ProsimFuelService(IProsimService prosimService, ILogger logger)
       {
           _prosimService = prosimService;
           _logger = logger;
       }
       
       // Implementation...
   }
   
   // Example: GSXFuelCoordinator as a consumer
   public class GSXFuelCoordinator : IGSXFuelCoordinator
   {
       private readonly IProsimFuelService _fuelService;
       
       public GSXFuelCoordinator(IProsimFuelService fuelService, /* other dependencies */)
       {
           _fuelService = fuelService;
       }
       
       // Implementation...
   }
   ```

3. **Make Changes Safely**

   Add new methods or modify existing methods while maintaining backward compatibility.

   ```csharp
   // Example: Adding a new method to IProsimFuelService
   public interface IProsimFuelService
   {
       Task<double> GetTotalFuelAsync();
       Task<bool> StartRefuelingAsync(double targetFuelKg);
       // New method
       Task<FuelDistribution> GetOptimalFuelDistributionAsync(double totalFuelKg);
       // Other methods...
   }
   
   // Example: Implementing the new method
   public class ProsimFuelService : IProsimFuelService
   {
       // Existing implementation...
       
       public async Task<FuelDistribution> GetOptimalFuelDistributionAsync(double totalFuelKg)
       {
           _logger.LogInformation($"Calculating optimal fuel distribution for {totalFuelKg} kg");
           
           try
           {
               // Implementation details...
               var maxCenterTank = await _prosimService.GetVariableAsync<double>("FUEL_TANK_CENTER_CAPACITY");
               var maxLeftTank = await _prosimService.GetVariableAsync<double>("FUEL_TANK_LEFT_CAPACITY");
               var maxRightTank = await _prosimService.GetVariableAsync<double>("FUEL_TANK_RIGHT_CAPACITY");
               
               // Calculate optimal distribution
               var distribution = CalculateOptimalDistribution(totalFuelKg, maxCenterTank, maxLeftTank, maxRightTank);
               
               return distribution;
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, $"Failed to calculate optimal fuel distribution for {totalFuelKg} kg");
               throw;
           }
       }
       
       private FuelDistribution CalculateOptimalDistribution(double totalFuelKg, double maxCenterTank, double maxLeftTank, double maxRightTank)
       {
           // Implementation details...
           // This is a simplified example
           var centerTank = Math.Min(totalFuelKg, maxCenterTank);
           var remainingFuel = totalFuelKg - centerTank;
           var leftTank = Math.Min(remainingFuel / 2, maxLeftTank);
           var rightTank = Math.Min(remainingFuel - leftTank, maxRightTank);
           
           return new FuelDistribution
           {
               CenterTankKg = centerTank,
               LeftTankKg = leftTank,
               RightTankKg = rightTank
           };
       }
   }
   ```

4. **Update Consumers**

   Update consumers to use the new functionality.

   ```csharp
   // Example: Using the new method in GSXFuelCoordinator
   public class GSXFuelCoordinator : IGSXFuelCoordinator
   {
       // Existing implementation...
       
       public async Task StartRefuelingWithOptimalDistributionAsync(double targetFuelKg)
       {
           _logger.LogInformation($"Starting refueling with optimal distribution for {targetFuelKg} kg");
           
           try
           {
               // Get optimal distribution
               var distribution = await _fuelService.GetOptimalFuelDistributionAsync(targetFuelKg);
               
               // Set fuel with optimal distribution
               await _fuelService.SetFuelAsync(distribution.CenterTankKg, distribution.LeftTankKg, distribution.RightTankKg);
               
               // Start refueling
               await _fuelService.StartRefuelingAsync(targetFuelKg);
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, $"Failed to start refueling with optimal distribution for {targetFuelKg} kg");
               throw;
           }
       }
   }
   ```

5. **Test Changes**

   Test the modified service to ensure it works as expected.

   ```csharp
   // Example: Unit test for the new method
   [TestMethod]
   public async Task GetOptimalFuelDistributionAsync_ReturnsValidDistribution()
   {
       // Arrange
       var prosimServiceMock = new Mock<IProsimService>();
       prosimServiceMock.Setup(m => m.GetVariableAsync<double>("FUEL_TANK_CENTER_CAPACITY")).ReturnsAsync(20000);
       prosimServiceMock.Setup(m => m.GetVariableAsync<double>("FUEL_TANK_LEFT_CAPACITY")).ReturnsAsync(10000);
       prosimServiceMock.Setup(m => m.GetVariableAsync<double>("FUEL_TANK_RIGHT_CAPACITY")).ReturnsAsync(10000);
       
       var loggerMock = new Mock<ILogger>();
       
       var fuelService = new ProsimFuelService(prosimServiceMock.Object, loggerMock.Object);
       
       // Act
       var distribution = await fuelService.GetOptimalFuelDistributionAsync(25000);
       
       // Assert
       Assert.AreEqual(20000, distribution.CenterTankKg);
       Assert.AreEqual(2500, distribution.LeftTankKg);
       Assert.AreEqual(2500, distribution.RightTankKg);
   }
   ```

### Considerations

- **Backward Compatibility**: Maintain backward compatibility when possible
- **Interface Segregation**: Consider creating a new interface for new functionality
- **Dependency Management**: Be aware of dependencies and consumers
- **Testing**: Test changes thoroughly
- **Documentation**: Update documentation to reflect changes

## Adding New Features

### Overview

Adding new features to Prosim2GSX involves identifying the appropriate service, implementing the feature, and integrating it with existing code.

### Steps

1. **Identify the Appropriate Service**

   Determine which service should contain the new feature.

   ```csharp
   // Example: Adding weather-based fuel consumption to ProsimFuelService
   public interface IProsimFuelService
   {
       // Existing methods...
       
       // New feature
       Task<double> CalculateFuelConsumptionAsync(FlightPlan flightPlan, WeatherData weather);
   }
   ```

2. **Implement the Feature**

   Implement the feature in the service.

   ```csharp
   // Example: Implementing the new feature
   public class ProsimFuelService : IProsimFuelService
   {
       // Existing implementation...
       
       public async Task<double> CalculateFuelConsumptionAsync(FlightPlan flightPlan, WeatherData weather)
       {
           _logger.LogInformation($"Calculating fuel consumption for flight {flightPlan.FlightNumber} with weather");
           
           try
           {
               // Implementation details...
               var distance = CalculateDistance(flightPlan.DepartureAirport, flightPlan.ArrivalAirport);
               var baseConsumption = distance * 0.1; // 0.1 kg per km
               
               // Adjust for weather
               var temperatureAdjustment = (weather.Temperature - 15) * 0.01; // 1% per degree above 15°C
               var windAdjustment = CalculateWindAdjustment(weather.WindSpeed, weather.WindDirection, flightPlan);
               
               var totalConsumption = baseConsumption * (1 + temperatureAdjustment + windAdjustment);
               
               return totalConsumption;
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, $"Failed to calculate fuel consumption for flight {flightPlan.FlightNumber}");
               throw;
           }
       }
       
       private double CalculateDistance(string departureAirport, string arrivalAirport)
       {
           // Implementation details...
           // This is a simplified example
           var airports = new Dictionary<string, (double Lat, double Lon)>
           {
               { "KSFO", (37.6213, -122.3790) },
               { "KLAX", (33.9416, -118.4085) },
               { "KJFK", (40.6413, -73.7781) }
           };
           
           if (!airports.TryGetValue(departureAirport, out var departure) ||
               !airports.TryGetValue(arrivalAirport, out var arrival))
           {
               throw new ArgumentException("Unknown airport");
           }
           
           var earthRadius = 6371; // km
           var dLat = ToRadians(arrival.Lat - departure.Lat);
           var dLon = ToRadians(arrival.Lon - departure.Lon);
           
           var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(departure.Lat)) * Math.Cos(ToRadians(arrival.Lat)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
           
           var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
           var distance = earthRadius * c;
           
           return distance;
       }
       
       private double CalculateWindAdjustment(double windSpeed, double windDirection, FlightPlan flightPlan)
       {
           // Implementation details...
           // This is a simplified example
           var flightDirection = CalculateFlightDirection(flightPlan.DepartureAirport, flightPlan.ArrivalAirport);
           var relativeDirection = Math.Abs(windDirection - flightDirection);
           
           if (relativeDirection > 180)
           {
               relativeDirection = 360 - relativeDirection;
           }
           
           // Headwind: 0 degrees, Tailwind: 180 degrees
           var headwindComponent = windSpeed * Math.Cos(ToRadians(relativeDirection));
           
           // Headwind increases consumption, tailwind decreases it
           return -headwindComponent * 0.005; // 0.5% per knot
       }
       
       private double CalculateFlightDirection(string departureAirport, string arrivalAirport)
       {
           // Implementation details...
           // This is a simplified example
           var airports = new Dictionary<string, (double Lat, double Lon)>
           {
               { "KSFO", (37.6213, -122.3790) },
               { "KLAX", (33.9416, -118.4085) },
               { "KJFK", (40.6413, -73.7781) }
           };
           
           if (!airports.TryGetValue(departureAirport, out var departure) ||
               !airports.TryGetValue(arrivalAirport, out var arrival))
           {
               throw new ArgumentException("Unknown airport");
           }
           
           var dLon = ToRadians(arrival.Lon - departure.Lon);
           var y = Math.Sin(dLon) * Math.Cos(ToRadians(arrival.Lat));
           var x = Math.Cos(ToRadians(departure.Lat)) * Math.Sin(ToRadians(arrival.Lat)) -
                   Math.Sin(ToRadians(departure.Lat)) * Math.Cos(ToRadians(arrival.Lat)) * Math.Cos(dLon);
           
           var bearing = ToDegrees(Math.Atan2(y, x));
           return (bearing + 360) % 360;
       }
       
       private double ToRadians(double degrees)
       {
           return degrees * Math.PI / 180;
       }
       
       private double ToDegrees(double radians)
       {
           return radians * 180 / Math.PI;
       }
   }
   ```

3. **Integrate with Existing Code**

   Integrate the new feature with existing code.

   ```csharp
   // Example: Using the new feature in GSXFuelCoordinator
   public class GSXFuelCoordinator : IGSXFuelCoordinator
   {
       private readonly IProsimFuelService _fuelService;
       private readonly IWeatherService _weatherService;
       private readonly IFlightPlanService _flightPlanService;
       
       public GSXFuelCoordinator(
           IProsimFuelService fuelService,
           IWeatherService weatherService,
           IFlightPlanService flightPlanService,
           /* other dependencies */)
       {
           _fuelService = fuelService;
           _weatherService = weatherService;
           _flightPlanService = flightPlanService;
       }
       
       public async Task CalculateAndSetFuelAsync()
       {
           try
           {
               // Get current flight plan
               var flightPlan = await _flightPlanService.GetCurrentFlightPlanAsync();
               
               // Get weather for departure airport
               var weather = await _weatherService.GetCurrentWeatherAsync(flightPlan.DepartureAirport);
               
               // Calculate fuel consumption
               var fuelConsumption = await _fuelService.CalculateFuelConsumptionAsync(flightPlan, weather);
               
               // Add reserve fuel (30 minutes)
               var reserveFuel = 30 * 60 * 0.05; // 0.05 kg per second
               var totalFuel = fuelConsumption + reserveFuel;
               
               // Set fuel
               await _fuelService.StartRefuelingAsync(totalFuel);
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Failed to calculate and set fuel");
               throw;
           }
       }
   }
   ```

4. **Test the Feature**

   Test the new feature to ensure it works as expected.

   ```csharp
   // Example: Unit test for the new feature
   [TestMethod]
   public async Task CalculateFuelConsumptionAsync_ReturnsValidConsumption()
   {
       // Arrange
       var prosimServiceMock = new Mock<IProsimService>();
       var loggerMock = new Mock<ILogger>();
       
       var fuelService = new ProsimFuelService(prosimServiceMock.Object, loggerMock.Object);
       
       var flightPlan = new FlightPlan
       {
           FlightNumber = "UA123",
           DepartureAirport = "KSFO",
           ArrivalAirport = "KLAX"
       };
       
       var weather = new WeatherData
       {
           Airport = "KSFO",
           Temperature = 20,
           WindSpeed = 10,
           WindDirection = 180
       };
       
       // Act
       var consumption = await fuelService.CalculateFuelConsumptionAsync(flightPlan, weather);
       
       // Assert
       Assert.IsTrue(consumption > 0);
       // Add more specific assertions based on expected behavior
   }
   ```

### Considerations

- **Feature Scope**: Define the scope of the feature clearly
- **Dependencies**: Identify and manage dependencies
- **Testing**: Test the feature thoroughly
- **Documentation**: Document the feature
- **User Experience**: Consider the user experience
- **Performance**: Evaluate performance implications

## Extending the State Machine

### Overview

Extending the state machine involves adding new states, defining state transitions, and implementing state-specific behavior.

### Steps

1. **Add New States**

   Add new states to the FlightState enum.

   ```csharp
   // Example: Adding a MAINTENANCE state
   public enum FlightState
   {
       PREFLIGHT,
       DEPARTURE,
       TAXIOUT,
       FLIGHT,
       TAXIIN,
       ARRIVAL,
       TURNAROUND,
       MAINTENANCE // New state
   }
   ```

2. **Define State Transitions**

   Define valid transitions to and from the new state.

   ```csharp
   // Example: Updating IsValidTransition method in GSXStateManager
   private bool IsValidTransition(FlightState currentState, FlightState newState)
   {
       switch (currentState)
       {
           case FlightState.PREFLIGHT:
               return newState == FlightState.DEPARTURE || newState == FlightState.MAINTENANCE;
           case FlightState.DEPARTURE:
               return newState == FlightState.TAXIOUT || newState == FlightState.MAINTENANCE;
           case FlightState.TAXIOUT:
               return newState == FlightState.FLIGHT || newState == FlightState.MAINTENANCE;
           case FlightState.FLIGHT:
               return newState == FlightState.TAXIIN;
           case FlightState.TAXIIN:
               return newState == FlightState.ARRIVAL || newState == FlightState.MAINTENANCE;
           case FlightState.ARRIVAL:
               return newState == FlightState.TURNAROUND || newState == FlightState.MAINTENANCE;
           case FlightState.TURNAROUND:
               return newState == FlightState.DEPARTURE || newState == FlightState.MAINTENANCE;
           case FlightState.MAINTENANCE:
               return newState == FlightState.PREFLIGHT || newState == FlightState.DEPARTURE || 
                      newState == FlightState.TAXIOUT || newState == FlightState.TAXIIN || 
                      newState == FlightState.ARRIVAL || newState == FlightState.TURNAROUND;
           default:
               return false;
       }
   }
   ```

3. **Implement State-Specific Behavior**

   Implement entry and exit actions for the new state.

   ```csharp
   // Example: Updating ExecuteEntryActions method in GSXStateManager
   private void ExecuteEntryActions(FlightState state)
   {
       switch (state)
       {
           case FlightState.PREFLIGHT:
               // Existing implementation...
               break;
           case FlightState.DEPARTURE:
               // Existing implementation...
               break;
           // Other existing states...
           case FlightState.MAINTENANCE:
               // Entry actions for MAINTENANCE state
               _logger.LogInformation("Entering MAINTENANCE state");
               
               // Notify maintenance service
               _maintenanceService?.StartMaintenanceMode();
               
               // Disable normal operations
               _serviceOrchestrator?.DisableServices();
               
               // Open maintenance doors
               _doorCoordinator?.OpenMaintenanceDoors();
               
               break;
       }
   }
   
   // Example: Updating ExecuteExitActions method in GSXStateManager
   private void ExecuteExitActions(FlightState state)
   {
       switch (state)
       {
           case FlightState.PREFLIGHT:
               // Existing implementation...
               break;
           case FlightState.DEPARTURE:
               // Existing implementation...
               break;
           // Other existing states...
           case FlightState.MAINTENANCE:
               // Exit actions for MAINTENANCE state
               _logger.LogInformation("Exiting MAINTENANCE state");
               
               // Notify maintenance service
               _maintenanceService?.EndMaintenanceMode();
               
               // Re-enable normal operations
               _serviceOrchestrator?.EnableServices();
               
               // Close maintenance doors
               _doorCoordinator?.CloseMaintenanceDoors();
               
               break;
       }
   }
   ```

4. **Update State Prediction**

   Update the state prediction logic to include the new state.

   ```csharp
   // Example: Updating PredictNextState method in GSXStateManager
   public FlightState PredictNextState(AircraftParameters parameters)
   {
       switch (CurrentState)
       {
           case FlightState.PREFLIGHT:
               if (parameters.HasFlightPlan)
                   return FlightState.DEPARTURE;
               if (parameters.MaintenanceRequested)
                   return FlightState.MAINTENANCE;
               break;
           case FlightState.DEPARTURE:
               if (!parameters.HasGroundEquipment)
                   return FlightState.TAXIOUT;
               if (parameters.MaintenanceRequested)
                   return FlightState.MAINTENANCE;
               break;
           // Other existing states...
           case FlightState.MAINTENANCE:
               if (parameters.MaintenanceComplete)
               {
                   if (parameters.HasFlightPlan)
                       return FlightState.DEPARTURE;
                   else
                       return FlightState.PREFLIGHT;
               }
               break;
       }
       
       return CurrentState; // No change predicted
   }
   ```

5. **Add State-Specific Services**

   Create services for the new state if needed.

   ```csharp
   // Example: Creating a maintenance service
   public interface IMaintenanceService
   {
       Task<bool> StartMaintenanceModeAsync();
       Task<bool> EndMaintenanceModeAsync();
       Task<bool> IsMaintenanceCompleteAsync();
       Task<MaintenanceStatus> GetMaintenanceStatusAsync();
       event EventHandler<MaintenanceStatusChangedEventArgs> MaintenanceStatusChanged;
   }
   
   public class MaintenanceService : IMaintenanceService
   {
       private readonly ILogger _logger;
       private readonly IProsimService _prosimService;
       private MaintenanceStatus _status;
       
       public event EventHandler<MaintenanceStatusChangedEventArgs> MaintenanceStatusChanged;
       
       public MaintenanceService(IProsimService prosimService, ILogger logger)
       {
           _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
           _logger = logger ?? throw new ArgumentNullException(nameof(logger));
           _status = MaintenanceStatus.Inactive;
       }
       
       public async Task<bool> StartMaintenanceModeAsync()
       {
           _logger.LogInformation("Starting maintenance mode");
           
           try
           {
               // Implementation details...
               await _prosimService.SetVariableAsync("MAINTENANCE_MODE", true);
               
               // Update status
               UpdateStatus(MaintenanceStatus.InProgress);
               
               return true;
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Failed to start maintenance mode");
               return false;
           }
       }
       
       public async Task<bool> EndMaintenanceModeAsync()
       {
           _logger.LogInformation("Ending maintenance mode");
           
           try
           {
               // Implementation details...
               await _prosimService.SetVariableAsync("MAINTENANCE_MODE", false);
               
               // Update status
               UpdateStatus(MaintenanceStatus.Complete);
               
               return true;
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Failed to end maintenance mode");
               return false;
           }
       }
       
       public async Task<bool> IsMaintenanceCompleteAsync()
       {
           try
           {
               var status = await GetMaintenanceStatusAsync();
               return status == MaintenanceStatus.Complete;
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Failed to check if maintenance is complete");
               return false;
           }
       }
       
       public async Task<MaintenanceStatus> GetMaintenanceStatusAsync()
       {
           try
           {
               // Implementation details...
               var isActive = await _prosimService.GetVariableAsync<bool>("MAINTENANCE_MODE");
               
               if (!isActive)
               {
                   if (_status == MaintenanceStatus.InProgress)
                   {
                       // Maintenance was in progress but is now inactive, so it's complete
                       Update
