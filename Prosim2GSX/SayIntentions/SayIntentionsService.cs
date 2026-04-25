using CFIT.AppLogger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public virtual bool IsActive
            => (AppService.Instance?.Config?.UseSayIntentions == true)
               && !string.IsNullOrWhiteSpace(ReadApiKey());

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

        public virtual async Task<SayIntentionsAssignResult> AssignGateAsync(string airportIcao, string gate)
        {
            var apiKey = ReadApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Logger.Debug("SayIntentions: not active (no api_key) — skipping assignGate");
                return new SayIntentionsAssignResult { Ok = false, Error = "SayIntentions not active." };
            }

            if (string.IsNullOrWhiteSpace(airportIcao) || string.IsNullOrWhiteSpace(gate))
            {
                Logger.Debug("SayIntentions: missing airport or gate — skipping assignGate");
                return new SayIntentionsAssignResult { Ok = false, Error = "Missing airport or gate." };
            }

            var normalisedGate = AlphaNumeric.Replace(gate, "").ToUpperInvariant();
            if (string.IsNullOrEmpty(normalisedGate))
            {
                Logger.Debug("SayIntentions: gate has no alphanumeric characters — skipping assignGate");
                return new SayIntentionsAssignResult { Ok = false, Error = "Gate has no alphanumeric characters." };
            }
            if (normalisedGate.Length > 30)
                normalisedGate = normalisedGate.Substring(0, 30);

            var normalisedIcao = airportIcao.Trim().ToUpperInvariant();

            try
            {
                var url = $"/sapi/assignGate?api_key={Uri.EscapeDataString(apiKey)}&gate={Uri.EscapeDataString(normalisedGate)}&airport={Uri.EscapeDataString(normalisedIcao)}";
                Logger.Debug($"SayIntentions: GET https://apipri.sayintentions.ai/sapi/assignGate?api_key=***&gate={normalisedGate}&airport={normalisedIcao}");
                using var response = await HttpClient.GetAsync(url, Token);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var status = (int)response.StatusCode;
                    Logger.Warning($"SayIntentions: assignGate HTTP {status} — body: {body}");
                    var friendly = status >= 500
                        ? $"SayIntentions service unavailable (HTTP {status}). Try again, or confirm a SayIntentions flight is active."
                        : $"HTTP {status}";
                    return new SayIntentionsAssignResult { Ok = false, Error = friendly };
                }

                var json = JsonNode.Parse(body);
                var error = json?["error"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Logger.Warning($"SayIntentions: assignGate returned error: {error}");
                    return new SayIntentionsAssignResult { Ok = false, Error = error };
                }

                var assigned = json?["assigned_gate_name"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(assigned))
                {
                    Logger.Warning($"SayIntentions: assignGate response missing assigned_gate_name — body: {body}");
                    return new SayIntentionsAssignResult { Ok = false, Error = "Response missing assigned_gate_name." };
                }

                Logger.Information($"SayIntentions: gate assigned at {normalisedIcao}: {assigned}");
                return new SayIntentionsAssignResult { Ok = true, AssignedGate = assigned };
            }
            catch (Exception ex)
            {
                Logger.Warning($"SayIntentions: assignGate failed ({ex.GetType().Name}: {ex.Message})");
                return new SayIntentionsAssignResult { Ok = false, Error = ex.Message };
            }
        }

        public virtual async Task<IReadOnlyList<SayIntentionsAirportWx>> GetWeatherAsync(IEnumerable<string> icaos)
        {
            var empty = (IReadOnlyList<SayIntentionsAirportWx>)Array.Empty<SayIntentionsAirportWx>();

            var apiKey = ReadApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Logger.Debug("SayIntentions: not active (no api_key) — skipping getWX");
                return empty;
            }

            var requested = (icaos ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();
            if (requested.Count == 0)
                return empty;

            try
            {
                var icaoParam = string.Join(",", requested);
                var url = $"/sapi/getWX?api_key={Uri.EscapeDataString(apiKey)}&icao={Uri.EscapeDataString(icaoParam)}";
                using var response = await HttpClient.GetAsync(url, Token);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warning($"SayIntentions: getWX HTTP {(int)response.StatusCode} — body: {body}");
                    return empty;
                }

                var json = JsonNode.Parse(body);
                var airports = json?["airports"] as JsonArray;
                if (airports == null)
                {
                    Logger.Warning($"SayIntentions: getWX response missing airports array — body: {body}");
                    return empty;
                }

                var result = new List<SayIntentionsAirportWx>(airports.Count);
                foreach (var node in airports)
                {
                    if (node == null) continue;
                    result.Add(new SayIntentionsAirportWx
                    {
                        Airport = node["airport"]?.GetValue<string>() ?? "",
                        Atis = node["atis"]?.GetValue<string>() ?? "",
                        Metar = node["metar"]?.GetValue<string>() ?? "",
                        Taf = node["taf"]?.GetValue<string>() ?? "",
                        ActiveRunway = node["active_runway"]?.GetValue<string>() ?? "",
                        WindDirection = TryGetInt(node["wind_direction"]),
                        WindSpeed = TryGetInt(node["wind_speed"]),
                    });
                }

                Logger.Information($"SayIntentions: getWX returned {result.Count} airport(s) for [{icaoParam}]");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Warning($"SayIntentions: getWX failed ({ex.GetType().Name}: {ex.Message})");
                return empty;
            }
        }

        protected static int? TryGetInt(JsonNode node)
        {
            if (node == null) return null;
            try { return node.GetValue<int>(); }
            catch { return null; }
        }
    }
}
