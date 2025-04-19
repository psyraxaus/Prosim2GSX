# Prosim Loadsheet Integration

This document describes the integration of Prosim's native loadsheet functionality into the Prosim2GSX application.

## Overview

The integration replaces the custom loadsheet calculation and formatting with Prosim's native loadsheet functionality. This ensures that the loadsheet data is consistent between Prosim and Prosim2GSX, and that the loadsheet is properly formatted and sent to the MCDU.

## Components

### ProsimLoadsheetService

A new service class that provides methods for:
- Generating loadsheets (preliminary and final)
- Resending loadsheets
- Resetting loadsheets
- Subscribing to loadsheet data changes

```csharp
public class ProsimLoadsheetService
{
    // Generate a loadsheet using Prosim's native functionality
    public async Task<bool> GenerateLoadsheet(string type)
    
    // Resend the current loadsheet to the MCDU
    public async Task<bool> ResendLoadsheet()
    
    // Reset all loadsheets
    public async Task<bool> ResetLoadsheets()
    
    // Subscribe to loadsheet data changes
    public void SubscribeToLoadsheetChanges()
}
```

### ProsimInterface Enhancements

The ProsimInterface class has been enhanced with HTTP request methods for interacting with the Prosim backend API:
- GetBackendUrl() - returns the hardcoded URL "http://127.0.0.1:5000/efb" for Prosim's backend
- PostAsync() - to make POST requests
- DeleteAsync() - to make DELETE requests

These methods are used by the ProsimLoadsheetService to communicate with Prosim's backend API.

### ProsimController Enhancements

The ProsimController class has been enhanced with methods for:
- Checking loadsheet availability
- Retrieving loadsheet data
- Exposing the ServiceModel property for use by the loadsheet service

### GsxController Changes

The GsxController class has been modified to:
- Initialize and use the ProsimLoadsheetService
- Generate loadsheets using Prosim's native functionality
- Send loadsheet data to ACARS if enabled

## Integration Flow

1. When a flight plan is loaded, the GsxController initializes the ProsimLoadsheetService.
2. During the departure phase, the GsxController calls the ProsimLoadsheetService to generate a preliminary loadsheet.
3. If ACARS is enabled, the GsxController retrieves the loadsheet data from Prosim and sends it to ACARS.
4. Before pushback, the GsxController calls the ProsimLoadsheetService to generate a final loadsheet.
5. If ACARS is enabled, the GsxController retrieves the loadsheet data from Prosim and sends it to ACARS.

## Benefits

- Consistent loadsheet data between Prosim and Prosim2GSX
- Proper formatting and sending of loadsheets to the MCDU
- Simplified codebase by removing complex weight and balance calculations
- Maintained compatibility with ACARS for external communication

## Removed Components

The following components are no longer needed and have been removed:
- A320WeightAndBalance.cs - Custom weight and balance calculator
- LoadsheetFormatter.cs - Custom loadsheet formatter
- Custom loadsheet calculation and formatting code in GsxController.cs

## Configuration

No additional configuration is required for the Prosim loadsheet integration. The integration uses the existing Prosim configuration.

## Troubleshooting

If you encounter issues with the loadsheet integration, check the following:
- Ensure that Prosim is running and connected
- Check that the flight plan is loaded correctly
- Verify that the Prosim backend API is accessible at http://127.0.0.1:5000/efb
- Check the logs for any error messages related to loadsheet generation or sending
- If you're experiencing connection issues, verify that Prosim's EFB server is running on port 5000
