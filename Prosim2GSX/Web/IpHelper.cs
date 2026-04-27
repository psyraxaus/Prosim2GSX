using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Prosim2GSX.Web
{
    // LAN address discovery for the QR-code panel. Filters out loopback,
    // tunnel, and down interfaces. The "best guess" pre-selection prefers
    // RFC1918 private ranges (10/8, 172.16/12, 192.168/16) which are what
    // home routers hand out — picks WiFi/Ethernet over Hyper-V virtual
    // switches, VPN adapters, and other operationally-up-but-not-LAN
    // interfaces, but the dropdown shows everything so the user can override
    // on multi-NIC machines.
    public static class IpHelper
    {
        public static List<string> GetLanIPv4Addresses()
        {
            var result = new List<(string Ip, NetworkInterfaceType Type)>();
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up) continue;
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                    var props = nic.GetIPProperties();
                    foreach (var addr in props.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        if (IPAddress.IsLoopback(addr.Address)) continue;
                        var ip = addr.Address.ToString();
                        if (!result.Any(x => x.Ip == ip))
                            result.Add((ip, nic.NetworkInterfaceType));
                    }
                }
            }
            catch { }

            // Sort: Ethernet/WiFi first, then everything else.
            int Score(NetworkInterfaceType t) => t switch
            {
                NetworkInterfaceType.Ethernet => 0,
                NetworkInterfaceType.GigabitEthernet => 0,
                NetworkInterfaceType.Wireless80211 => 1,
                _ => 2,
            };
            var sorted = result
                .OrderBy(x => Score(x.Type))
                .ThenBy(x => IsPrivate(x.Ip) ? 0 : 1)
                .Select(x => x.Ip)
                .ToList();

            if (sorted.Count == 0) sorted.Add("127.0.0.1");
            return sorted;
        }

        public static string BestGuessLanIp()
        {
            var all = GetLanIPv4Addresses();
            return all.FirstOrDefault(IsPrivate) ?? all.FirstOrDefault() ?? "127.0.0.1";
        }

        private static bool IsPrivate(string ip)
        {
            if (!IPAddress.TryParse(ip, out var addr)) return false;
            var bytes = addr.GetAddressBytes();
            if (bytes.Length != 4) return false;
            if (bytes[0] == 10) return true;
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            return false;
        }
    }
}
