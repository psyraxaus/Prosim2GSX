using CFIT.AppLogger;
using Newtonsoft.Json;
using Prosim2GSX.UI.Views.Checklists;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Prosim2GSX.Checklists
{
    public interface IChecklistService
    {
        IReadOnlyList<string> GetAvailableChecklists();
        ChecklistDefinition LoadChecklist(string name);
        string DefaultChecklistName { get; }
    }

    public class ChecklistService : IChecklistService
    {
        protected const string DefaultName = "a320_default";
        protected const string EmbeddedResourceName = "Prosim2GSX.UI.Views.Checklists.Resources.a320_default.json";

        protected virtual string FolderPath { get; }

        public virtual string DefaultChecklistName => DefaultName;

        public ChecklistService()
        {
            FolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Prosim2GSX",
                "Checklists");

            EnsureFolderAndDefault();
        }

        protected virtual void EnsureFolderAndDefault()
        {
            try
            {
                Directory.CreateDirectory(FolderPath);
                var defaultPath = Path.Combine(FolderPath, DefaultName + ".json");
                if (!File.Exists(defaultPath))
                {
                    var content = ReadEmbeddedDefault();
                    if (content != null)
                    {
                        File.WriteAllText(defaultPath, content);
                        Logger.Information($"ChecklistService: wrote default checklist to {defaultPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "ChecklistService: failed to ensure default checklist file");
            }
        }

        protected virtual string ReadEmbeddedDefault()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream(EmbeddedResourceName);
                if (stream == null)
                {
                    Logger.Warning($"ChecklistService: embedded resource not found: {EmbeddedResourceName}");
                    return null;
                }
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "ChecklistService: failed to read embedded default checklist");
                return null;
            }
        }

        public virtual IReadOnlyList<string> GetAvailableChecklists()
        {
            try
            {
                if (!Directory.Exists(FolderPath))
                    return Array.Empty<string>();

                return Directory.EnumerateFiles(FolderPath, "*.json", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileNameWithoutExtension)
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "ChecklistService: failed to enumerate checklists");
                return Array.Empty<string>();
            }
        }

        public virtual ChecklistDefinition LoadChecklist(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = DefaultName;

            try
            {
                var path = Path.Combine(FolderPath, name + ".json");
                if (!File.Exists(path))
                {
                    Logger.Warning($"ChecklistService: checklist not found: {path}; falling back to embedded default");
                    var content = ReadEmbeddedDefault();
                    return content != null ? JsonConvert.DeserializeObject<ChecklistDefinition>(content) : null;
                }
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<ChecklistDefinition>(json);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"ChecklistService: failed to load checklist '{name}'");
                return null;
            }
        }
    }
}
