using CFIT.AppLogger;
using Prosim2GSX.Checklists;
using Prosim2GSX.State;
using Prosim2GSX.UI.Views.Checklists;
using Prosim2GSX.Web.Contracts;
using Prosim2GSX.Web.Contracts.Commands;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Prosim2GSX.Commands.Handlers
{
    // Checklists command handlers. All handlers mutate AppService.Checklist
    // (the long-lived store) and return the post-mutation full snapshot so
    // the calling client doesn't have to wait for the WS delta to arrive.
    public static class ChecklistHandlers
    {
        public static void Register(AppService app, CommandRegistry registry)
        {
            registry.Register<SelectChecklistRequest, ChecklistCommandResponse>(
                "checklists.select",
                (req, ct) => Select(app, req));

            registry.Register<SelectSectionRequest, ChecklistCommandResponse>(
                "checklists.selectSection",
                (req, ct) => SelectSection(app, req));

            registry.Register<ToggleItemRequest, ChecklistCommandResponse>(
                "checklists.toggleItem",
                (req, ct) => ToggleItem(app, req));

            registry.Register<ResetSectionRequest, ChecklistCommandResponse>(
                "checklists.resetSection",
                (req, ct) => ResetSection(app, req));

            registry.Register<CompleteSectionRequest, ChecklistCommandResponse>(
                "checklists.completeSection",
                (req, ct) => CompleteSection(app));
        }

        private static Task<ChecklistCommandResponse> Select(AppService app, SelectChecklistRequest req)
        {
            var name = (req?.Name ?? "").Trim();
            if (string.IsNullOrEmpty(name))
                throw new CommandValidationException("Checklist name must not be empty.");

            try
            {
                var def = app.ChecklistService?.LoadChecklist(name);
                if (def == null)
                    throw new CommandValidationException($"Checklist '{name}' not found.");
                app.Checklist?.LoadDefinition(def, name);

                var profile = app.GsxService?.AircraftProfile;
                if (profile != null)
                {
                    profile.ChecklistName = name;
                    try { app.Config?.SaveConfiguration(); } catch (Exception ex) { Logger.LogException(ex); }
                }
            }
            catch (CommandValidationException) { throw; }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return Task.FromResult(new ChecklistCommandResponse { Snapshot = ChecklistDto.From(app) });
        }

        private static Task<ChecklistCommandResponse> SelectSection(AppService app, SelectSectionRequest req)
        {
            var s = app.Checklist;
            if (s?.Definition?.Sections == null)
                return Task.FromResult(new ChecklistCommandResponse { Snapshot = ChecklistDto.From(app) });
            var idx = req?.SectionIndex ?? -1;
            if (idx < 0 || idx >= s.Definition.Sections.Count)
                throw new CommandValidationException("Section index out of range.");
            s.CurrentSectionIndex = idx;
            s.RecomputeCurrentItem();
            return Task.FromResult(new ChecklistCommandResponse { Snapshot = ChecklistDto.From(app) });
        }

        private static Task<ChecklistCommandResponse> ToggleItem(AppService app, ToggleItemRequest req)
        {
            var s = app.Checklist;
            if (s == null || req == null) throw new CommandValidationException("Invalid request.");
            if (!s.ItemsBySection.TryGetValue(req.SectionIndex, out var items))
                throw new CommandValidationException("Section index out of range.");
            if (req.ItemIndex < 0 || req.ItemIndex >= items.Count)
                throw new CommandValidationException("Item index out of range.");
            var rt = items[req.ItemIndex];
            if (rt.Definition.IsNote || rt.Definition.IsSeparator)
                throw new CommandValidationException("Item is not toggleable.");
            bool isManual = string.IsNullOrWhiteSpace(rt.Definition.DataRef)
                            && (rt.Definition.DataRefs == null || rt.Definition.DataRefs.Count == 0);
            bool overrideAllowed = app.Config?.AllowManualChecklistOverride ?? false;
            if (!isManual && !overrideAllowed)
                throw new CommandValidationException("Dataref-driven items cannot be toggled manually (enable 'Allow Manual Checklist Override' in Integrations to permit).");
            rt.IsChecked = !rt.IsChecked;
            s.RecomputeCurrentItem();
            return Task.FromResult(new ChecklistCommandResponse { Snapshot = ChecklistDto.From(app) });
        }

        private static Task<ChecklistCommandResponse> ResetSection(AppService app, ResetSectionRequest req)
        {
            var s = app.Checklist;
            if (s == null || req == null) throw new CommandValidationException("Invalid request.");
            if (!s.ItemsBySection.ContainsKey(req.SectionIndex))
                throw new CommandValidationException("Section index out of range.");
            s.ResetSection(req.SectionIndex);
            return Task.FromResult(new ChecklistCommandResponse { Snapshot = ChecklistDto.From(app) });
        }

        private static Task<ChecklistCommandResponse> CompleteSection(AppService app)
        {
            var s = app.Checklist;
            if (s == null) return Task.FromResult(new ChecklistCommandResponse { Snapshot = ChecklistDto.From(app) });
            if (!s.ItemsBySection.TryGetValue(s.CurrentSectionIndex, out var items))
                return Task.FromResult(new ChecklistCommandResponse { Snapshot = ChecklistDto.From(app) });
            foreach (var rt in items)
            {
                if (rt.Definition.IsNote || rt.Definition.IsSeparator) continue;
                if (!rt.IsChecked) rt.IsChecked = true;
            }
            if (s.Definition?.Sections != null && s.CurrentSectionIndex < s.Definition.Sections.Count - 1)
            {
                s.CurrentSectionIndex++;
                s.RecomputeCurrentItem();
            }
            return Task.FromResult(new ChecklistCommandResponse { Snapshot = ChecklistDto.From(app) });
        }
    }
}
