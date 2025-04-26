using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Prosim2GSX.Services.Logger.Enums;
using Prosim2GSX.Services.Logger.Implementation;

namespace Prosim2GSX.Services
{
    public class AcarsClient
    {

        private int messageCounter = 0;
        private bool isErrorState = false;

        private HttpClient httpClient;

        private Regex messageRegex = new Regex(@"\{(\S*)\s(\S*)\s\{(\/\S*\/|TELEX\s)([^\}]*)\}\}");
        public string Callsign { get; set; }
        public string AcarsNetworkUrl { get; set; }
        public string LogonSecret { get; set; }


        public AcarsClient(string callsign, string logonSecret, string acarsNetworkUrl)
        {
            if (logonSecret == null || logonSecret.Length < 6)
                throw new ArgumentException("Invalid logon secret.");
            if (callsign == null || callsign.Length < 3)
                throw new ArgumentException("Invalid callsign.");

            Callsign = callsign;
            LogonSecret = logonSecret;
            AcarsNetworkUrl = acarsNetworkUrl;

            this.httpClient = new HttpClient();
            this.httpClient.Timeout = TimeSpan.FromSeconds(5);

        }

        public async Task SendMessageToAcars(string toCallsign, string messageType, string packetData)
        {
            var connectionValues = new Dictionary<string, string> {
                {"logon", LogonSecret},
                {"from", Callsign},
                {"to", toCallsign},
                {"type", messageType},
                {"packet", packetData}
            };

            var content = new FormUrlEncodedContent(connectionValues);

            try
            {
                var response = await httpClient.PostAsync(AcarsNetworkUrl, content);

                LogService.Log(LogLevel.Debug, "AcarsClient:SendMessageToAcars", $"PACKET SENT: {toCallsign} | {messageType} | {packetData} ");

                var responseString = await response.Content.ReadAsStringAsync();
                string printString = responseString.ToString().ToUpper().Trim();

                LogService.Log(LogLevel.Debug, "AcarsClient:SendMessageToAcars", $"RECEIVED: {responseString}");

                if (printString.Contains("ERROR"))
                {
                    throw new HttpRequestException();
                }
            }
            catch (Exception e)
            {
                if (!isErrorState)
                {
                    LogService.Log(LogLevel.Debug, "AcarsClient:SendMessageToAcars", $"{e.GetType().FullName}: {e.Message}");
                    isErrorState = true;
                }
            }
        }

        public void SetCallsign(string callsign)
        {
            Callsign = callsign;
        }
    }
}
