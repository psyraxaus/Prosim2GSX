using Prosim2GSX.AppConfig;
using System.Collections.Generic;
using System.Linq;

namespace Prosim2GSX.Audio
{
    // Validates the multi-ACP VoiceMeeter mapping config before the binder
    // wires up subscriptions. Two invariants are enforced:
    //   - Within each active ACP: no two mappings share the same Channel.
    //   - Across all active ACPs: no two mappings share the same (StripIndex, IsBus).
    //
    // VHF1 on ACP1 → Strip 1 AND VHF1 on ACP2 → Strip 5 is legal (two pilots
    // independently driving separate strips from their own VHF1 knob). What's
    // illegal is two mappings pointing at the same strip — VoiceMeeter would
    // see racing writes from different ACP knobs.
    public static class VoiceMeeterMappingValidator
    {
        public class ValidationResult
        {
            public bool IsValid => string.IsNullOrEmpty(Message);
            public string Message { get; set; }
        }

        public static ValidationResult Validate(Config cfg)
        {
            var conflicts = new List<string>();
            var stripOwner = new Dictionary<(int idx, bool isBus), (AcpSide acp, AudioChannel ch)>();

            foreach (var acp in cfg.ActiveAcps ?? Enumerable.Empty<AcpSide>())
            {
                if (!cfg.VoiceMeeterMappingsByAcp.TryGetValue(acp, out var mappings) || mappings == null)
                    continue;

                // Within-ACP channel duplicates.
                var channelSeen = new HashSet<AudioChannel>();
                foreach (var m in mappings)
                {
                    if (!channelSeen.Add(m.Channel))
                        conflicts.Add($"ACP{(int)acp + 1} has duplicate channel {m.Channel}");
                }

                // Across-ACP strip duplicates.
                foreach (var m in mappings)
                {
                    var key = (m.StripIndex, m.IsBus);
                    if (stripOwner.TryGetValue(key, out var existing))
                    {
                        string target = $"{(m.IsBus ? "Bus" : "Strip")} {m.StripIndex + 1}";
                        conflicts.Add($"{target} used by both ACP{(int)existing.acp + 1}.{existing.ch} and ACP{(int)acp + 1}.{m.Channel}");
                    }
                    else
                    {
                        stripOwner[key] = (acp, m.Channel);
                    }
                }
            }

            return new ValidationResult
            {
                Message = conflicts.Count > 0 ? string.Join("; ", conflicts) : null,
            };
        }
    }
}
