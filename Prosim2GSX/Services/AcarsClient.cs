﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
            if (string.IsNullOrWhiteSpace(acarsNetworkUrl))
                throw new ArgumentException("ACARS network URL cannot be null or empty.");

            Callsign = callsign;
            LogonSecret = logonSecret;
            
            // Ensure the URL is an absolute URI
            if (!acarsNetworkUrl.StartsWith("http://") && !acarsNetworkUrl.StartsWith("https://"))
            {
                acarsNetworkUrl = "https://" + acarsNetworkUrl;
                Logger.Log(LogLevel.Warning, "AcarsClient:Constructor", $"ACARS URL did not include protocol, prepending 'https://': {acarsNetworkUrl}");
            }
            
            // Validate that it's a well-formed URI
            if (!Uri.TryCreate(acarsNetworkUrl, UriKind.Absolute, out _))
            {
                Logger.Log(LogLevel.Error, "AcarsClient:Constructor", $"Invalid ACARS network URL: {acarsNetworkUrl}");
                throw new ArgumentException($"Invalid ACARS network URL: {acarsNetworkUrl}");
            }
            
            AcarsNetworkUrl = acarsNetworkUrl;

            this.httpClient = new HttpClient();
            this.httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public async Task SendMessageToAcars(string toCallsign, string messageType, string packetData)
        {
            if (string.IsNullOrWhiteSpace(AcarsNetworkUrl))
            {
                Logger.Log(LogLevel.Error, "AcarsClient:SendMessageToAcars", "ACARS network URL is not set");
                throw new InvalidOperationException("ACARS network URL is not set");
            }

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
                Logger.Log(LogLevel.Debug, "AcarsClient:SendMessageToAcars", $"Sending to URL: {AcarsNetworkUrl}");
                var response = await httpClient.PostAsync(AcarsNetworkUrl, content);

                Logger.Log(LogLevel.Debug, "AcarsClient:SendMessageToAcars", $"PACKET SENT: {toCallsign} | {messageType} | {packetData} ");

                var responseString = await response.Content.ReadAsStringAsync();
                string printString = responseString.ToString().ToUpper().Trim();

                Logger.Log(LogLevel.Debug, "AcarsClient:SendMessageToAcars", $"RECEIVED: {responseString}");

                if (printString.Contains("ERROR"))
                {
                    throw new HttpRequestException($"ACARS server returned error: {responseString}");
                }
            }
            catch (Exception e)
            {
                if (!isErrorState)
                {
                    Logger.Log(LogLevel.Debug, "AcarsClient:SendMessageToAcars", $"{e.GetType().FullName}: {e.Message}");
                    isErrorState = true;
                }
                throw; // Re-throw to allow proper handling upstream
            }
        }

        public void SetCallsign(string callsign)
        {
            Callsign = callsign;
        }
    }
}
