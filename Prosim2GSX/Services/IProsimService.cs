using System;

namespace Prosim2GSX.Services
{
    public class ProsimConnectionEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public string Message { get; }

        public ProsimConnectionEventArgs(bool isConnected, string message)
        {
            IsConnected = isConnected;
            Message = message;
        }
    }

    public class ProsimDataChangedEventArgs : EventArgs
    {
        public string DataRef { get; }
        public object Value { get; }

        public ProsimDataChangedEventArgs(string dataRef, object value)
        {
            DataRef = dataRef;
            Value = value;
        }
    }

    public interface IProsimService
    {
        bool IsConnected { get; }
        
        void Connect(string hostname);
        
        dynamic ReadDataRef(string dataRef);
        
        void SetVariable(string dataRef, object value);
        
        event EventHandler<ProsimConnectionEventArgs> ConnectionChanged;
    }
}
