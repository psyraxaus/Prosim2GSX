using System;

namespace Prosim2GSX.Services
{
    public class ProsimDoorService : IProsimDoorService
    {
        private readonly IProsimService _prosimService;
        
        public event EventHandler<DoorStateChangedEventArgs> DoorStateChanged;
        
        public ProsimDoorService(IProsimService prosimService)
        {
            _prosimService = prosimService ?? throw new ArgumentNullException(nameof(prosimService));
        }
        
        public void SetAftRightDoor(bool open)
        {
            _prosimService.SetVariable("doors.entry.right.aft", open);
            Logger.Log(LogLevel.Information, "ProsimDoorService:SetAftRightDoor", $"Aft right door {(open ? "opened" : "closed")}");
            OnDoorStateChanged("AftRightDoor", open);
        }
        
        public void SetForwardRightDoor(bool open)
        {
            _prosimService.SetVariable("doors.entry.right.fwd", open);
            Logger.Log(LogLevel.Information, "ProsimDoorService:SetForwardRightDoor", $"Forward right door {(open ? "opened" : "closed")}");
            OnDoorStateChanged("ForwardRightDoor", open);
        }
        
        public void SetForwardCargoDoor(bool open)
        {
            _prosimService.SetVariable("doors.cargo.forward", open);
            Logger.Log(LogLevel.Information, "ProsimDoorService:SetForwardCargoDoor", $"Forward cargo door {(open ? "opened" : "closed")}");
            OnDoorStateChanged("ForwardCargoDoor", open);
        }
        
        public void SetAftCargoDoor(bool open)
        {
            _prosimService.SetVariable("doors.cargo.aft", open);
            Logger.Log(LogLevel.Information, "ProsimDoorService:SetAftCargoDoor", $"Aft cargo door {(open ? "opened" : "closed")}");
            OnDoorStateChanged("AftCargoDoor", open);
        }
        
        protected virtual void OnDoorStateChanged(string doorName, bool isOpen)
        {
            DoorType doorType;
            switch (doorName)
            {
                case "ForwardRightDoor":
                    doorType = DoorType.ForwardRight;
                    break;
                case "AftRightDoor":
                    doorType = DoorType.AftRight;
                    break;
                case "ForwardCargoDoor":
                    doorType = DoorType.ForwardCargo;
                    break;
                case "AftCargoDoor":
                    doorType = DoorType.AftCargo;
                    break;
                default:
                    Logger.Log(LogLevel.Warning, "ProsimDoorService:OnDoorStateChanged", $"Unknown door name: {doorName}");
                    return;
            }
            
            DoorStateChanged?.Invoke(this, new DoorStateChangedEventArgs(doorType, isOpen));
        }
    }
}
