# Prosim2GSX Unit Testing Strategy

## Overview

This document outlines the strategy for implementing unit tests for the Prosim2GSX application after completing the modularization process. The goal is to ensure that all services and components are thoroughly tested, improving code quality, reliability, and maintainability.

## Testing Framework and Tools

Based on the existing test infrastructure, we will use:

- **MSTest**: The primary testing framework
- **Moq**: For mocking dependencies
- **FluentAssertions** (optional addition): For more readable assertions

## Test Project Structure

```
Prosim2GSX/
└── Tests/
    ├── Services/
    │   ├── SimConnectServiceTests.cs
    │   ├── ProsimServiceTests.cs (existing)
    │   ├── AcarsServiceTests.cs
    │   ├── FlightPlanServiceTests.cs
    │   ├── ProsimDoorServiceTests.cs
    │   ├── ProsimEquipmentServiceTests.cs
    │   ├── ProsimPassengerServiceTests.cs
    │   ├── ProsimCargoServiceTests.cs
    │   ├── ProsimFuelServiceTests.cs
    │   ├── ProsimFlightDataServiceTests.cs
    │   ├── ProsimFluidServiceTests.cs
    │   ├── GSXMenuServiceTests.cs
    │   ├── GSXAudioServiceTests.cs
    │   └── GSXStateManagerTests.cs
    ├── Controllers/
    │   ├── GsxControllerTests.cs
    │   ├── ProsimControllerTests.cs
    │   └── ServiceControllerTests.cs
    └── TestHelpers/
        ├── MockFactory.cs
        └── TestData.cs
```

## Testing Approach for Each Service

### 1. Constructor Tests

- Verify proper initialization
- Validate parameter validation (null checks, etc.)
- Ensure dependencies are correctly stored

Example:

```csharp
[TestMethod]
public void Constructor_WithNullModel_ThrowsArgumentNullException()
{
    Assert.ThrowsException<ArgumentNullException>(() => new FlightPlanService(null));
}

[TestMethod]
public void Constructor_WithValidParameters_InitializesCorrectly()
{
    // Arrange
    var mockModel = new Mock<ServiceModel>();
    
    // Act
    var service = new FlightPlanService(mockModel.Object);
    
    // Assert
    Assert.IsNotNull(service);
    // Additional assertions as needed
}
```

### 2. Method Tests

- Test each public method with various inputs
- Verify correct behavior for normal cases
- Test error handling for edge cases
- Verify return values and state changes

Example:

```csharp
[TestMethod]
public async Task LoadFlightPlanAsync_WithValidResponse_ReturnsTrue()
{
    // Arrange
    SetupMockHttpResponse(GetValidFlightPlanXml());
    
    // Act
    bool result = await _service.LoadFlightPlanAsync();
    
    // Assert
    Assert.IsTrue(result);
}

[TestMethod]
public async Task LoadFlightPlanAsync_WithHttpError_ReturnsFalse()
{
    // Arrange
    SetupMockHttpError();
    
    // Act
    bool result = await _service.LoadFlightPlanAsync();
    
    // Assert
    Assert.IsFalse(result);
}
```

### 3. Event Tests

- Verify events are raised correctly
- Test event arguments contain expected data
- Ensure events are not raised when they shouldn't be

Example:

```csharp
[TestMethod]
public async Task LoadFlightPlanAsync_WithNewFlightPlan_RaisesFlightPlanLoadedEvent()
{
    // Arrange
    SetupMockHttpResponse(GetValidFlightPlanXml());
    bool eventRaised = false;
    FlightPlanEventArgs capturedArgs = null;
    
    _service.FlightPlanLoaded += (s, e) => 
    {
        eventRaised = true;
        capturedArgs = e;
    };
    
    // Act
    await _service.LoadFlightPlanAsync();
    
    // Assert
    Assert.IsTrue(eventRaised);
    Assert.IsNotNull(capturedArgs);
    Assert.AreEqual("ABC123", capturedArgs.FlightPlanId);
    Assert.AreEqual("EDDF", capturedArgs.Origin);
    Assert.AreEqual("EGLL", capturedArgs.Destination);
}
```

### 4. Integration with Dependencies

- Test interaction with dependencies using mocks
- Verify correct behavior when dependencies fail
- Test complex interactions between components

Example:

```csharp
[TestMethod]
public async Task GetFlightPlanDataAsync_CallsFetchOnlineFlightPlanAsync()
{
    // Arrange
    var mockService = new Mock<IFlightPlanService>();
    mockService.Setup(s => s.FetchOnlineFlightPlanAsync())
               .ReturnsAsync(CreateMockXmlNode());
    
    // Act
    var result = await mockService.Object.GetFlightPlanDataAsync();
    
    // Assert
    mockService.Verify(s => s.FetchOnlineFlightPlanAsync(), Times.Once);
    Assert.IsNotNull(result);
}
```

## Implementation Timeline

### Phase 1: Complete Tests for Existing Services

1. **FlightPlanServiceTests.cs**
   - Priority: High (marked as missing in modularization strategy)
   - Focus on testing HTTP requests, XML parsing, and event raising

2. **ProsimDoorServiceTests.cs**
   - Priority: High (recently implemented service)
   - Test door state management and ProSim SDK interactions

3. **Enhance ProsimServiceTests.cs**
   - Priority: Medium (existing but may need expansion)
   - Add more test cases for comprehensive coverage

### Phase 2: Add Tests for Future Services as They're Implemented

1. **ProsimEquipmentServiceTests.cs**
   - Test equipment state management
   - Verify correct interaction with ProSim SDK

2. **ProsimPassengerServiceTests.cs**
   - Test passenger boarding/deboarding logic
   - Verify passenger count calculations

3. **ProsimCargoServiceTests.cs**
   - Test cargo loading/unloading
   - Verify weight calculations

4. **ProsimFuelServiceTests.cs**
   - Test refueling operations
   - Verify fuel rate calculations

5. **ProsimFlightDataServiceTests.cs**
   - Test flight data retrieval
   - Verify data formatting

6. **ProsimFluidServiceTests.cs**
   - Test fluid management
   - Verify state persistence

### Phase 3: GSX Service Tests

1. **GSXMenuServiceTests.cs**
   - Test menu navigation
   - Verify operator selection

2. **GSXAudioServiceTests.cs**
   - Test audio control
   - Verify volume adjustments

3. **GSXStateManagerTests.cs**
   - Test state transitions
   - Verify state-specific behavior

### Phase 4: Controller Tests

1. **GsxControllerTests.cs**
   - Test coordination between services
   - Verify state management

2. **ProsimControllerTests.cs**
   - Test ProSim integration
   - Verify data synchronization

3. **ServiceControllerTests.cs**
   - Test service lifecycle management
   - Verify initialization order

## Detailed Implementation Examples

### FlightPlanServiceTests.cs

```csharp
[TestClass]
public class FlightPlanServiceTests
{
    private Mock<ServiceModel> _mockModel;
    private FlightPlanService _service;
    private HttpMessageHandler _mockHttpMessageHandler;
    
    [TestInitialize]
    public void Setup()
    {
        _mockModel = new Mock<ServiceModel>();
        _mockModel.Setup(m => m.SimBriefID).Returns("12345");
        _mockModel.Setup(m => m.SimBriefURL).Returns("https://www.simbrief.com/api/xml.fetcher.php?userid={0}");
        
        // Setup mock HTTP handler
        _mockHttpMessageHandler = SetupMockHttpMessageHandler();
        var httpClient = new HttpClient(_mockHttpMessageHandler);
        
        _service = new FlightPlanService(_mockModel.Object, httpClient);
    }
    
    [TestMethod]
    public void Constructor_WithNullModel_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new FlightPlanService(null));
    }
    
    [TestMethod]
    public async Task LoadFlightPlanAsync_WithValidResponse_ReturnsTrue()
    {
        // Arrange
        SetupMockHttpResponse(GetValidFlightPlanXml());
        bool eventRaised = false;
        _service.FlightPlanLoaded += (s, e) => eventRaised = true;
        
        // Act
        bool result = await _service.LoadFlightPlanAsync();
        
        // Assert
        Assert.IsTrue(result);
        Assert.IsTrue(eventRaised);
    }
    
    [TestMethod]
    public async Task LoadFlightPlanAsync_WithSameFlightPlan_ReturnsFalse()
    {
        // Arrange
        SetupMockHttpResponse(GetValidFlightPlanXml());
        await _service.LoadFlightPlanAsync(); // Load first time
        
        bool eventRaised = false;
        _service.FlightPlanLoaded += (s, e) => eventRaised = true;
        
        // Act
        bool result = await _service.LoadFlightPlanAsync(); // Load second time
        
        // Assert
        Assert.IsFalse(result); // Should return false for same flight plan
        Assert.IsFalse(eventRaised); // Event should not be raised
    }
    
    [TestMethod]
    public async Task LoadFlightPlanAsync_WithHttpError_ReturnsFalse()
    {
        // Arrange
        SetupMockHttpError();
        
        // Act
        bool result = await _service.LoadFlightPlanAsync();
        
        // Assert
        Assert.IsFalse(result);
    }
    
    [TestMethod]
    public async Task FetchOnlineFlightPlanAsync_WithValidResponse_ReturnsXmlNode()
    {
        // Arrange
        SetupMockHttpResponse(GetValidFlightPlanXml());
        
        // Act
        var result = await _service.FetchOnlineFlightPlanAsync();
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("ABC123", result["params"]["request_id"].InnerText);
    }
    
    [TestMethod]
    public async Task FetchOnlineFlightPlanAsync_WithInvalidSimBriefId_ReturnsNull()
    {
        // Arrange
        _mockModel.Setup(m => m.SimBriefID).Returns("0");
        
        // Act
        var result = await _service.FetchOnlineFlightPlanAsync();
        
        // Assert
        Assert.IsNull(result);
    }
    
    // Helper methods for setting up mock HTTP responses
    private HttpMessageHandler SetupMockHttpMessageHandler()
    {
        // Implementation details
    }
    
    private void SetupMockHttpResponse(string content)
    {
        // Implementation details
    }
    
    private void SetupMockHttpError()
    {
        // Implementation details
    }
    
    private string GetValidFlightPlanXml()
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
                <OFP>
                    <params>
                        <request_id>ABC123</request_id>
                    </params>
                    <general>
                        <icao_airline>BA</icao_airline>
                        <flight_number>123</flight_number>
                    </general>
                    <origin>
                        <icao_code>EDDF</icao_code>
                    </origin>
                    <destination>
                        <icao_code>EGLL</icao_code>
                    </destination>
                </OFP>";
    }
}
```

### ProsimDoorServiceTests.cs

```csharp
[TestClass]
public class ProsimDoorServiceTests
{
    private Mock<IProsimService> _mockProsimService;
    private ProsimDoorService _service;
    
    [TestInitialize]
    public void Setup()
    {
        _mockProsimService = new Mock<IProsimService>();
        _service = new ProsimDoorService(_mockProsimService.Object);
    }
    
    [TestMethod]
    public void Constructor_WithNullProsimService_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => new ProsimDoorService(null));
    }
    
    [TestMethod]
    public void SetForwardRightDoor_CallsProsimServiceWithCorrectParameters()
    {
        // Arrange
        bool doorState = true;
        
        // Act
        _service.SetForwardRightDoor(doorState);
        
        // Assert
        _mockProsimService.Verify(p => p.SetVariable("DOOR_FWD_RIGHT", doorState ? 1 : 0), Times.Once);
    }
    
    // Additional test methods for other doors and functionality
}
```

## Testing Challenges and Solutions

### 1. External Dependencies

**Challenge**: Services like FlightPlanService depend on external systems (HTTP requests)

**Solutions**:
- Use mock HTTP handlers to simulate responses
- Create test-specific implementations of HttpClient
- Use dependency injection to provide test doubles

Example:
```csharp
private HttpMessageHandler CreateMockHandler(HttpResponseMessage response)
{
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(response);
    return mockHandler.Object;
}
```

### 2. SimConnect and ProSim SDK

**Challenge**: Testing code that interacts with SimConnect and ProSim SDK

**Solutions**:
- Create interfaces/wrappers around SDK calls that can be mocked
- Use dependency injection to provide test implementations
- Create test-specific service implementations

Example:
```csharp
public interface IProSimSdk
{
    bool Connect(string hostname);
    void SetVariable(string name, int value);
    int GetVariable(string name);
}

public class ProSimSdkWrapper : IProSimSdk
{
    // Real implementation that calls the SDK
}

public class TestProSimSdk : IProSimSdk
{
    // Test implementation for unit tests
}
```

### 3. Event-Based Communication

**Challenge**: Testing event-based interactions between components

**Solutions**:
- Use event handlers in tests to verify events are raised correctly
- Capture event arguments for verification
- Test both positive and negative cases (events raised and not raised)

Example:
```csharp
[TestMethod]
public void WhenStateChanges_EventIsRaised()
{
    // Arrange
    bool eventRaised = false;
    _service.StateChanged += (s, e) => eventRaised = true;
    
    // Act
    _service.ChangeState(newState);
    
    // Assert
    Assert.IsTrue(eventRaised);
}
```

### 4. Asynchronous Code

**Challenge**: Testing async methods properly

**Solutions**:
- Use async/await in test methods
- Use appropriate assertions for async code
- Test timeout scenarios

Example:
```csharp
[TestMethod]
public async Task AsyncMethod_CompletesSuccessfully()
{
    // Arrange
    SetupAsyncDependencies();
    
    // Act
    var result = await _service.AsyncMethodUnderTest();
    
    // Assert
    Assert.IsNotNull(result);
}
```

## Test Coverage Goals

- **Services**: 80%+ code coverage
- **Controllers**: 70%+ code coverage
- **Core Logic**: 90%+ code coverage
- **Edge Cases**: Test all error handling paths

## Benefits of This Approach

1. **Improved Code Quality**: Tests help identify bugs and issues early
2. **Better Maintainability**: Tests document expected behavior
3. **Safer Refactoring**: Tests provide confidence when making changes
4. **Enhanced Collaboration**: Tests clarify component responsibilities
5. **Regression Prevention**: Tests catch regressions when making changes

## Next Steps

1. **Implement FlightPlanServiceTests.cs**
   - Create the test class
   - Implement test methods
   - Verify test coverage

2. **Implement ProsimDoorServiceTests.cs**
   - Create the test class
   - Implement test methods
   - Verify test coverage

3. **Update Existing Tests**
   - Enhance ProsimServiceTests.cs
   - Ensure consistent testing approach

4. **Continue with Remaining Services**
   - Implement tests as services are completed
   - Maintain consistent testing patterns

5. **Integrate with CI/CD**
   - Run tests automatically on commits
   - Track test coverage over time
