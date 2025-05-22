using Prosim2GSX.Events;
using Prosim2GSX.Services.PTT.Enums;

public class PttChannelConfigChangedEvent : EventBase
{
    public AcpChannelType ChannelType { get; }
    public bool IsEnabled { get; }

    public PttChannelConfigChangedEvent(AcpChannelType channelType, bool isEnabled)
    {
        ChannelType = channelType;
        IsEnabled = isEnabled;
    }
}
