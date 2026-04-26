using Prosim2GSX.AppConfig;
using Prosim2GSX.GSX.Services;
using System;

namespace Prosim2GSX.Web.Contracts
{
    // One entry of a profile's DepartureServices array. Mirrors ServiceConfig
    // 1:1 except MinimumFlightDuration becomes minimumFlightDurationSeconds
    // (number) on the wire via TimeSpanSecondsConverter.
    public class ServiceConfigDto
    {
        public GsxServiceType ServiceType { get; set; } = GsxServiceType.Unknown;
        public GsxServiceActivation ServiceActivation { get; set; } = GsxServiceActivation.Manual;
        public GsxServiceConstraint ServiceConstraint { get; set; } = GsxServiceConstraint.NoneAlways;
        public TimeSpan MinimumFlightDuration { get; set; } = TimeSpan.Zero;

        public static ServiceConfigDto From(ServiceConfig src) => new()
        {
            ServiceType = src.ServiceType,
            ServiceActivation = src.ServiceActivation,
            ServiceConstraint = src.ServiceConstraint,
            MinimumFlightDuration = src.MinimumFlightDuration,
        };

        public ServiceConfig ToServiceConfig() => new()
        {
            ServiceType = ServiceType,
            ServiceActivation = ServiceActivation,
            ServiceConstraint = ServiceConstraint,
            MinimumFlightDuration = MinimumFlightDuration,
        };
    }
}
