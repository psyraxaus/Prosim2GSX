using System;
using System.Collections.Generic;
using CFIT.AppLogger;
using Prosim2GSX.Aircraft;

namespace Prosim2GSX.AppConfig
{
    // Pure aircraft-profile matching, extracted off Config so the algorithm is
    // testable without the live AppService.Instance service graph. Returns the
    // first Title-then-Airline match, or null when none matches — the caller
    // (Config.GetAircraftProfile) owns the default-profile fallback + CheckServices.
    public static class ProfileMatcher
    {
        public static AircraftProfile Match(AircraftInterface aircraft, IEnumerable<AircraftProfile> profiles, bool isMsfs2024)
        {
            if (aircraft == null || !aircraft.IsLoaded || profiles == null)
                return null;

            // Title / livery match takes precedence.
            foreach (var profile in profiles)
            {
                if (profile.MatchType != ProfileMatchType.Title)
                    continue;
                foreach (var s in profile.MatchString.Split('|'))
                {
                    if (aircraft.Title.Contains(s, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Logger.Information($"Loading Profile '{profile.Name}' (matched on Title/Livery - '{aircraft.Title}' contains '{s}')");
                        return profile;
                    }
                }
            }

            // Then Airline (MSFS2020) / livery-in-Title (MSFS2024).
            foreach (var profile in profiles)
            {
                if (profile.MatchType != ProfileMatchType.Airline)
                    continue;
                foreach (var s in profile.MatchString.Split('|'))
                {
                    if (!isMsfs2024 && aircraft.Airline.StartsWith(s, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Logger.Information($"Loading Profile '{profile.Name}' (matched on Airline - '{aircraft.Airline}' starts with '{s}')");
                        return profile;
                    }
                    else if (isMsfs2024 && aircraft.Title.Contains(s, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Logger.Information($"Loading Profile '{profile.Name}' (matched on Livery - '{aircraft.Title}' contains '{s}')");
                        return profile;
                    }
                }
            }

            return null;
        }
    }
}
