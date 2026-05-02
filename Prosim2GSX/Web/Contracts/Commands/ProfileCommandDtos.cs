using Prosim2GSX.AppConfig;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Prosim2GSX.Web.Contracts.Commands
{
    // ── Requests ────────────────────────────────────────────────────────

    public class SetActiveProfileRequest
    {
        public string Name { get; set; } = "";
    }

    public class CloneProfileRequest
    {
        public string SourceName { get; set; } = "";
        // Empty → server auto-names "Clone of <SourceName>". Non-empty
        // values are validated against the existing profile collection.
        public string NewName { get; set; } = "";
    }

    public class RenameProfileRequest
    {
        public string OldName { get; set; } = "";
        public string NewName { get; set; } = "";
    }

    public class UpdateProfileMetadataRequest
    {
        public string Name { get; set; } = "";
        public ProfileMatchType MatchType { get; set; } = ProfileMatchType.Default;
        public string MatchString { get; set; } = "";
    }

    public class DeleteProfileRequest
    {
        public string Name { get; set; } = "";
    }

    // ── Responses ───────────────────────────────────────────────────────

    public class ProfileSummaryDto
    {
        public string Name { get; set; } = "";
        public ProfileMatchType MatchType { get; set; }
        public string MatchString { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }

        public static ProfileSummaryDto From(AircraftProfile profile, string activeName)
        {
            return new ProfileSummaryDto
            {
                Name = profile.Name ?? "",
                MatchType = profile.MatchType,
                MatchString = profile.MatchString ?? "",
                IsActive = string.Equals(profile.Name, activeName, StringComparison.OrdinalIgnoreCase),
                IsDefault = string.Equals(profile.Name, "default", StringComparison.OrdinalIgnoreCase),
            };
        }
    }

    // Every profile command returns the full list because mutations
    // (rename, set-active, delete) can change the active flag on multiple
    // entries — returning the whole list lets the React panel re-render
    // once instead of patching individual rows.
    public class ProfilesListDto
    {
        public string ActiveName { get; set; } = "";
        public List<ProfileSummaryDto> Profiles { get; set; } = new();

        // "Current Aircraft" card on the web Profiles panel mirrors the WPF
        // surface: airline (ICAO from OFP) + title/livery + active profile.
        // Registration removed 2026-05-02 — the dataref is unreliable on this
        // aircraft variant, so registration-based matching was retired too.
        public string CurrentAirline { get; set; } = "";
        public string CurrentTitle { get; set; } = "";
        public string CurrentProfile { get; set; } = "";

        public static ProfilesListDto From(AppService app)
        {
            var active = app?.GsxService?.AircraftProfile?.Name ?? "";
            var list = (app?.Config?.AircraftProfiles ?? new List<AircraftProfile>())
                .Select(p => ProfileSummaryDto.From(p, active))
                .ToList();
            var aircraft = app?.GsxService?.AircraftInterface;
            return new ProfilesListDto
            {
                ActiveName = active,
                Profiles = list,
                CurrentAirline = aircraft?.Airline ?? "",
                CurrentTitle = aircraft?.Title ?? "",
                CurrentProfile = app?.GsxService?.AircraftProfile?.ToString() ?? "",
            };
        }
    }
}
