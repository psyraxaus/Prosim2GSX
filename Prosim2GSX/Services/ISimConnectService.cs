using System;

namespace Prosim2GSX.Services
{
    /// <summary>
    /// Interface for SimConnect service operations
    /// </summary>
    public interface ISimConnectService : IDisposable
    {
        // Connection state
        bool IsConnected { get; }
        bool IsReady { get; }
        bool IsGsxMenuReady { get; set; }
        
        // Connection management
        bool Connect();
        void Disconnect();
        
        // Variable subscription
        void SubscribeLvar(string address);
        void SubscribeSimVar(string name, string unit);
        void SubscribeEnvVar(string name, string unit);
        void UnsubscribeAll();
        
        // Variable reading
        float ReadLvar(string address);
        float ReadSimVar(string name, string unit);
        float ReadEnvVar(string name, string unit);
        
        // Variable writing
        void WriteLvar(string address, float value);
        void ExecuteCode(string code);
    }
}
