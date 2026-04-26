using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prosim2GSX.Web.Contracts
{
    // Shared JSON serializer configuration for the web layer (REST + WebSocket).
    // All web-facing serialization must use WebJsonOptions.Default so the wire
    // contract stays consistent — no controller or handler should construct
    // its own JsonSerializerOptions.
    //
    // Conventions (locked in project memory project_web_json_contract_decisions):
    //   - Property names    : camelCase (PropertyNamingPolicy)
    //   - Enums             : string names AS DECLARED (preserves acronyms like
    //                         CPT/VHF1/INT/UNKNOWN — a CamelCase naming policy
    //                         would mangle these to cPT/vHF1/etc.)
    //   - TimeSpan          : total seconds via TimeSpanSecondsConverter
    //   - Dictionary keys   : preserved as-is (registrations like "VH-ABC" must
    //                         not be camel-cased — DictionaryKeyPolicy left null)
    //   - Null on write     : omitted (DefaultIgnoreCondition = WhenWritingNull)
    public static class WebJsonOptions
    {
        public static JsonSerializerOptions Default { get; } = Build();

        private static JsonSerializerOptions Build()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new TimeSpanSecondsConverter());
            return options;
        }
    }
}
