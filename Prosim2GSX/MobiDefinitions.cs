using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Prosim2GSX
{
    public enum MOBIFLIGHT_CLIENT_DATA_ID
    {
        MOBIFLIGHT_LVARS,
        MOBIFLIGHT_CMD,
        MOBIFLIGHT_RESPONSE
    }

    public enum PILOTSDECK_CLIENT_DATA_ID
    {
        MOBIFLIGHT_LVARS = 1988,
        MOBIFLIGHT_CMD,
        MOBIFLIGHT_RESPONSE
    }

    public enum SIMCONNECT_REQUEST_ID
    {
        Dummy = 0
    }

    public enum SIMCONNECT_DEFINE_ID
    {
        Dummy = 0
    }

    public enum SIMCONNECT_NOTIFICATION_GROUP_ID
    {
        SIMCONNECT_GROUP_PRIORITY_DEFAULT,
        SIMCONNECT_GROUP_PRIORITY_HIGHEST
    }

    public enum SIM_EVENTS
    {
        EXTERNAL_SYSTEM_TOGGLE
    };

    public enum NOTFIY_GROUP
    {
        GROUP0
    };

    public class SimVar
    {
        public UInt32 ID { get; set; }
        public String Name { get; set; }
        public float Data { get; set; }

        public SimVar(uint iD, float data = 0.0f)
        {
            ID = iD;
            Data = data;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ClientDataValue
    {
        public float data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ClientDataString
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)MobiSimConnect.MOBIFLIGHT_MESSAGE_SIZE)]
        public byte[] data;

        public ClientDataString()
        {
            data = new byte[MobiSimConnect.MOBIFLIGHT_MESSAGE_SIZE];
        }

        public ClientDataString(string strData) : this()
        {
            SetData(strData);
        }
        
        // Add constructor that accepts ReadOnlySpan<char> for better performance
        public ClientDataString(ReadOnlySpan<char> strData) : this()
        {
            SetData(strData);
        }
        
        // Add method to set data from string
        public void SetData(string strData)
        {
            SetData(strData.AsSpan());
        }
        
        // Add method to set data from ReadOnlySpan<char>
        public void SetData(ReadOnlySpan<char> strData)
        {
            // Clear existing data
            Array.Clear(data, 0, data.Length);
            
            // Check if the input data will fit in the destination buffer
            if (strData.Length >= MobiSimConnect.MOBIFLIGHT_MESSAGE_SIZE)
            {
                // If input is too large, truncate it to fit
                strData = strData.Slice(0, (int)MobiSimConnect.MOBIFLIGHT_MESSAGE_SIZE - 1);
            }
            
            // Convert directly from Span<char> to avoid string allocation
            Encoding.ASCII.GetBytes(strData, data);
        }
    }

    public struct ResponseString
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)MobiSimConnect.MOBIFLIGHT_MESSAGE_SIZE)]
        public String Data;
    }
}
