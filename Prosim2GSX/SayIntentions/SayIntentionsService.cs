using CFIT.AppLogger;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.SayIntentions
{
    public class SayIntentionsService : ISayIntentionsService
    {
        protected static readonly Regex AlphaNumeric = new("[^A-Za-z0-9]", RegexOptions.Compiled);
        protected virtual string FlightFilePath { get; }
        protected virtual HttpClient HttpClient { get; }
        protected virtual CancellationToken Token => AppService.Instance?.Token ?? CancellationToken.None;

        public SayIntentionsService()
        {
            FlightFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SayIntentionsAI",
                "flight.json");

            HttpClient = new HttpClient
            {
                BaseAddress = new Uri("https://apipri.sayintentions.ai"),
                Timeout = TimeSpan.FromSeconds(15),
            };
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public virtual bool IsActive => !string.IsNullOrWhiteSpace(ReadApiKey());

        protected virtual string ReadApiKey()
        {
            try
            {
                if (!File.Exists(FlightFilePath))
                    return null;

                var json = JsonNode.Parse(File.ReadAllText(FlightFilePath));
                var key = json?["flight_details"]?["api_key"]?.GetValue<string>();
                return string.IsNullOrWhiteSpace(key) ? null : key;
            }
            catch (Exception ex)
            {
                Logger.Debug($"SayIntentions: failed to read flight.json ({ex.GetType().Name}: {ex.Message})");
                return null;
            }
        }

        public virtual async Task<bool> AssignGateAsync(string airportIcao, string gate)
        {
            var apiKey = ReadApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Logger.Debug("SayIntentions: not active (no api_key) — skipping assignGate");
                return false;
            }

            if (string.IsNullOrWhiteSpace(airportIcao) || string.IsNullOrWhiteSpace(gate))
            {
                Logger.Debug("SayIntentions: missing airport or gate — skipping assignGate");
                return false;
            }

            var normalisedGate = AlphaNumeric.Replace(gate, "").ToUpperInvariant();
            if (string.IsNullOrEmpty(normalisedGate))
            {
                Logger.Debug("SayIntentions: gate has no alphanumeric characters — skipping assignGate");
                return false;
            }
            if (normalisedGate.Length > 30)
                normalisedGate = normalisedGate.Substring(0, 30);

            var normalisedIcao = airportIcao.Trim().ToUpperInvariant();

            try
            {
                var url = $"/sapi/assignGate?api_key={Uri.EscapeDataString(apiKey)}&gate={Uri.EscapeDataString(normalisedGate)}&airport={Uri.EscapeDataString(normalisedIcao)}";
                using var response = await HttpClient.GetAsync(url, Token);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warning($"SayIntentions: assignGate HTTP {(int)response.StatusCode} — body: {body}");
                    return false;
                }

                var json = JsonNode.Parse(body);
                var error = json?["error"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Logger.Warning($"SayIntentions: assignGate returned error: {error}");
                    return false;
                }

                var assigned = json?["assigned_gate_name"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(assigned))
                {
                    Logger.Warning($"SayIntentions: assignGate response missing assigned_gate_name — body: {body}");
                    return false;
                }

                Logger.Information($"SayIntentions: gate assigned at {normalisedIcao}: {assigned}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"SayIntentions: assignGate failed ({ex.GetType().Name}: {ex.Message})");
                return false;
            }
        }
    }
}
