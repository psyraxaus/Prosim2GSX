using Microsoft.Extensions.Logging;
using Prosim2GSX.Services.Prosim.Interfaces;
using System;

namespace Prosim2GSX.Services.Prosim.Implementation
{
    public class DoorControlService : IDoorControlService
    {
        private readonly IProsimInterface _prosimService;
        private readonly ILogger<DoorControlService> _logger;

        public DoorControlService(ILogger<DoorControlService> logger, IProsimInterface prosimService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
        }

        public void SetForwardRightDoor(bool open)
        {
            _prosimService.SetProsimVariable("doors.entry.right.fwd", open);
            _logger.LogInformation("Forward right door {Status}", open ? "opened" : "closed");
        }

        public string GetForwardRightDoor()
        {
            bool doorStatus = _prosimService.GetProsimVariable("doors.entry.right.fwd");
            string status = doorStatus ? "open" : "closed";
            _logger.LogDebug("Forward right door {Status}", status);
            return status;
        }

        public void SetAftRightDoor(bool open)
        {
            _prosimService.SetProsimVariable("doors.entry.right.aft", open);
            _logger.LogInformation("Aft right door {Status}", open ? "opened" : "closed");
        }

        public string GetAftRightDoor()
        {
            bool doorStatus = _prosimService.GetProsimVariable("doors.entry.right.aft");
            string status = doorStatus ? "open" : "closed";
            _logger.LogDebug("Aft right door {Status}", status);
            return status;
        }

        public void SetForwardCargoDoor(bool open)
        {
            _prosimService.SetProsimVariable("doors.cargo.forward", open);
            _logger.LogInformation("Forward cargo door {Status}", open ? "opened" : "closed");
        }

        public string GetForwardCargoDoor()
        {
            bool doorStatus = _prosimService.GetProsimVariable("doors.cargo.forward");
            string status = doorStatus ? "open" : "closed";
            return status;
        }

        public void SetAftCargoDoor(bool open)
        {
            _prosimService.SetProsimVariable("doors.cargo.aft", open);
            _logger.LogInformation("Aft cargo door {Status}", open ? "opened" : "closed");
        }

        public string GetAftCargoDoor()
        {
            bool doorStatus = _prosimService.GetProsimVariable("doors.cargo.aft");
            string status = doorStatus ? "open" : "closed";
            return status;
        }
    }
}
