using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prosim2GSX.Web.Contracts
{
    // Serializes TimeSpan as a JSON number of total seconds (double). Avoids
    // the default System.Text.Json format ("00:00:30") which is awkward to
    // consume in JS/TS. On read, also tolerates standard TimeSpan strings so
    // a copy-pasted "00:00:30" from the C# config would still round-trip.
    public class TimeSpanSecondsConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetDouble(out var seconds))
                return TimeSpan.FromSeconds(seconds);

            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (!string.IsNullOrEmpty(s) && TimeSpan.TryParse(s, out var ts))
                    return ts;
            }

            throw new JsonException($"Expected a number of seconds or a TimeSpan string for TimeSpan, got {reader.TokenType}.");
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.TotalSeconds);
        }
    }
}
