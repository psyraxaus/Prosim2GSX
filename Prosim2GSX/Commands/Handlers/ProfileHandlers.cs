using Prosim2GSX.AppConfig;
using Prosim2GSX.Web.Contracts.Commands;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Commands.Handlers
{
    // Aircraft Profile CRUD command handlers. All operations mutate
    // Config.AircraftProfiles in place and call SaveConfiguration; mutations
    // to GsxController.AircraftProfile (set-active) flow through
    // SetAircraftProfile so the existing ProfileChanged event fires and the
    // WPF GSX Settings tab refreshes.
    //
    // Every handler returns ProfilesListDto so the React panel can re-render
    // the full list once after each operation — simpler than per-row patches
    // when set-active / rename / delete can change multiple flags at once.
    public static class ProfileHandlers
    {
        public static void Register(AppService app, CommandRegistry registry)
        {
            registry.Register<SetActiveProfileRequest, ProfilesListDto>(
                "profiles.setActive",
                (req, ct) => SetActive(app, req, ct));

            registry.Register<CloneProfileRequest, ProfilesListDto>(
                "profiles.clone",
                (req, ct) => Clone(app, req, ct));

            registry.Register<RenameProfileRequest, ProfilesListDto>(
                "profiles.rename",
                (req, ct) => Rename(app, req, ct));

            registry.Register<UpdateProfileMetadataRequest, ProfilesListDto>(
                "profiles.updateMetadata",
                (req, ct) => UpdateMetadata(app, req, ct));

            registry.Register<DeleteProfileRequest, ProfilesListDto>(
                "profiles.delete",
                (req, ct) => Delete(app, req, ct));
        }

        // ── setActive ───────────────────────────────────────────────────────

        private static Task<ProfilesListDto> SetActive(
            AppService app, SetActiveProfileRequest req, CancellationToken _)
        {
            ValidateName(req?.Name, "Profile name required.");
            var profile = FindProfile(app, req.Name)
                ?? throw new CommandValidationException($"Profile '{req.Name}' not found.");

            app.GsxService?.SetAircraftProfile(profile.Name);
            return Task.FromResult(ProfilesListDto.From(app));
        }

        // ── clone ───────────────────────────────────────────────────────────

        private static Task<ProfilesListDto> Clone(
            AppService app, CloneProfileRequest req, CancellationToken _)
        {
            ValidateName(req?.SourceName, "Source profile name required.");
            var source = FindProfile(app, req.SourceName)
                ?? throw new CommandValidationException($"Profile '{req.SourceName}' not found.");

            var newName = string.IsNullOrWhiteSpace(req.NewName)
                ? $"Clone of {source.Name}"
                : req.NewName.Trim();

            if (FindProfile(app, newName) != null)
                throw new CommandValidationException($"Profile '{newName}' already exists.");

            var json = JsonSerializer.Serialize(source);
            var clone = JsonSerializer.Deserialize<AircraftProfile>(json)
                ?? throw new CommandException("Failed to clone profile.");
            clone.Name = newName;

            // Cloning the default profile produces a copy that can never be
            // selected by the matcher (Default match wins last). Switch the
            // clone to Airline so the user can pick it via match-string.
            if (source.MatchType == ProfileMatchType.Default)
                clone.MatchType = ProfileMatchType.Airline;

            app.Config.AircraftProfiles.Add(clone);
            app.Config.SaveConfiguration();
            return Task.FromResult(ProfilesListDto.From(app));
        }

        // ── rename ──────────────────────────────────────────────────────────

        private static Task<ProfilesListDto> Rename(
            AppService app, RenameProfileRequest req, CancellationToken _)
        {
            ValidateName(req?.OldName, "Old profile name required.");
            ValidateName(req.NewName, "New profile name required.");

            var profile = FindProfile(app, req.OldName)
                ?? throw new CommandValidationException($"Profile '{req.OldName}' not found.");

            // Refuse to rename the default profile — its identity is referenced
            // by string in Config.GetAircraftProfile's fallback path, so
            // renaming would silently break profile matching.
            if (string.Equals(profile.Name, "default", StringComparison.OrdinalIgnoreCase))
                throw new CommandValidationException("The default profile cannot be renamed.");

            var trimmedNew = req.NewName.Trim();
            if (string.Equals(req.OldName, trimmedNew, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(ProfilesListDto.From(app));
            if (FindProfile(app, trimmedNew) != null)
                throw new CommandValidationException($"Profile '{trimmedNew}' already exists.");

            var wasActive = string.Equals(
                app.GsxService?.AircraftProfile?.Name, profile.Name, StringComparison.OrdinalIgnoreCase);

            profile.Name = trimmedNew;
            app.Config.SaveConfiguration();

            // Re-point the active profile so the GsxController doesn't keep
            // pointing at a name that no longer exists in the list.
            if (wasActive)
                app.GsxService?.SetAircraftProfile(trimmedNew);

            return Task.FromResult(ProfilesListDto.From(app));
        }

        // ── updateMetadata ──────────────────────────────────────────────────

        private static Task<ProfilesListDto> UpdateMetadata(
            AppService app, UpdateProfileMetadataRequest req, CancellationToken _)
        {
            ValidateName(req?.Name, "Profile name required.");
            var profile = FindProfile(app, req.Name)
                ?? throw new CommandValidationException($"Profile '{req.Name}' not found.");

            // The default profile must keep MatchType=Default so the
            // fallback path in Config.GetAircraftProfile keeps working.
            if (string.Equals(profile.Name, "default", StringComparison.OrdinalIgnoreCase)
                && req.MatchType != ProfileMatchType.Default)
            {
                throw new CommandValidationException(
                    "The default profile must keep the Default match-type.");
            }

            profile.MatchType = req.MatchType;
            profile.MatchString = req.MatchString ?? "";
            app.Config.SaveConfiguration();
            return Task.FromResult(ProfilesListDto.From(app));
        }

        // ── delete ──────────────────────────────────────────────────────────

        private static Task<ProfilesListDto> Delete(
            AppService app, DeleteProfileRequest req, CancellationToken _)
        {
            ValidateName(req?.Name, "Profile name required.");
            var profile = FindProfile(app, req.Name)
                ?? throw new CommandValidationException($"Profile '{req.Name}' not found.");

            if (string.Equals(profile.Name, "default", StringComparison.OrdinalIgnoreCase))
                throw new CommandValidationException("The default profile cannot be deleted.");

            // Refuse to delete the active profile — the GsxController would
            // hold a dangling reference and the profile-matching logic would
            // break on next aircraft load. Caller must SetActive elsewhere first.
            if (string.Equals(app.GsxService?.AircraftProfile?.Name, profile.Name, StringComparison.OrdinalIgnoreCase))
                throw new CommandValidationException(
                    "Cannot delete the currently-active profile. Set another profile active first.");

            app.Config.AircraftProfiles.Remove(profile);
            app.Config.SaveConfiguration();
            return Task.FromResult(ProfilesListDto.From(app));
        }

        // ── helpers ─────────────────────────────────────────────────────────

        private static AircraftProfile FindProfile(AppService app, string name)
        {
            if (app?.Config?.AircraftProfiles == null) return null;
            return app.Config.AircraftProfiles.FirstOrDefault(
                p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static void ValidateName(string name, string message)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new CommandValidationException(message);
        }
    }
}
