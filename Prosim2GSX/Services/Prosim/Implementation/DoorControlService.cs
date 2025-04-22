using Prosim2GSX.Services.Prosim.Interfaces;
using System;

namespace Prosim2GSX.Services.Prosim.Implementation
{
    public class DoorControlService : IDoorControlService
    {
        private readonly IProsimInterface _prosimService;

        public DoorControlService(IProsimInterface prosimService)
        {
            _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
        }

        public void SetForwardRightDoor(bool open)
        {
            _prosimService.SetProsimVariable("doors.entry.right.fwd", open);
            Logger.Log(LogLevel.Information, nameof(DoorControlService),
                $"Forward right door {(open ? "opened" : "closed")}");
        }

        public string GetForwardRightDoor()
        {
            bool doorStatus = _prosimService.GetProsimVariable("doors.entry.right.fwd");
            string status = doorStatus ? "open" : "closed";
            Logger.Log(LogLevel.Debug, nameof(DoorControlService),
                $"Forward right door {status}");
            return status;
        }

        public void SetAftRightDoor(bool open)
        {
            _prosimService.SetProsimVariable("doors.entry.right.aft", open);
            Logger.Log(LogLevel.Information, nameof(DoorControlService),
                $"Aft right door {(open ? "opened" : "closed")}");
        }

        public string GetAftRightDoor()
        {
            bool doorStatus = _prosimService.GetProsimVariable("doors.entry.right.aft");
            string status = doorStatus ? "open" : "closed";
            Logger.Log(LogLevel.Debug, nameof(DoorControlService),
                $"Aft right door {status}");
            return status;
        }

        public void SetForwardCargoDoor(bool open)
        {
            _prosimService.SetProsimVariable("doors.cargo.forward", open);
            Logger.Log(LogLevel.Information, nameof(DoorControlService),
                $"Forward cargo door {(open ? "opened" : "closed")}");
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
            Logger.Log(LogLevel.Information, nameof(DoorControlService),
                $"Aft cargo door {(open ? "opened" : "closed")}");
        }

        public string GetAftCargoDoor()
        {
            bool doorStatus = _prosimService.GetProsimVariable("doors.cargo.aft");
            string status = doorStatus ? "open" : "closed";
            return status;
        }
    }
}