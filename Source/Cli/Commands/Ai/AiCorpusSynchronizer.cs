// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cratis.Cli.Commands.Ai;

/// <summary>Synchronizes only the files recorded as Cratis-owned in a project.</summary>
public static class AiCorpusSynchronizer
{
    const string ConfigurationPath = ".cratis/ai.json";
    const string ManifestPath = ".cratis/ai.manifest.json";
    static readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };

    /// <summary>Installs or updates the selected corpus, preserving changed and unknown files.</summary>
    /// <param name="projectPath">The consuming repository root.</param>
    /// <param name="corpusPath">The Cratis AI repository root.</param>
    /// <param name="configuration">The requested harnesses, profiles, and languages.</param>
    /// <param name="force">Whether modified managed files may be replaced or removed.</param>
    /// <returns>The changes made and files requiring user attention.</returns>
    public static SyncResult Synchronize(string projectPath, string corpusPath, AiConfiguration configuration, bool force = false)
    {
        var previous = ReadManifest(projectPath);
        var desired = Resolve(corpusPath, configuration).ToDictionary(asset => asset.Destination, StringComparer.Ordinal);
        var conflicts = ModifiedFiles(projectPath, previous);
        if (conflicts.Count > 0 && !force) return new([], conflicts);
        conflicts.Clear();
        var actions = new List<string>();
        var installed = new List<AiManagedFile>();

        foreach (var asset in desired.Values)
        {
            var destination = Path.Combine(projectPath, ".cratis", "ai", asset.Destination);
            var content = AddMarker(asset.Source, File.ReadAllText(asset.Path));
            var hash = Hash(content);
            var existed = File.Exists(destination);
            var existing = previous.Files.FirstOrDefault(file => file.Destination == asset.Destination);
            if (!force && existed && existing is not null && !string.Equals(Hash(File.ReadAllText(destination)), existing.Hash, StringComparison.Ordinal))
            {
                conflicts.Add(asset.Destination);
                installed.Add(existing);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.WriteAllText(destination, content);
            installed.Add(new(asset.Source, asset.Destination, hash));
            actions.Add(existed ? $"Updated {asset.Destination}" : $"Added {asset.Destination}");
        }

        foreach (var existing in previous.Files.Where(file => !desired.ContainsKey(file.Destination)))
        {
            var destination = Path.Combine(projectPath, ".cratis", "ai", existing.Destination);
            if (!File.Exists(destination)) continue;
            if (!force && !string.Equals(Hash(File.ReadAllText(destination)), existing.Hash, StringComparison.Ordinal))
            {
                conflicts.Add(existing.Destination);
                installed.Add(existing);
                continue;
            }

            File.Delete(destination);
            actions.Add($"Removed {existing.Destination}");
        }

        ConfigureHarnesses(projectPath, configuration.Harnesses, installed.Select(file => file.Destination));
        WriteJson(Path.Combine(projectPath, ConfigurationPath), new { schemaVersion = AiConfiguration.SchemaVersion, harnesses = configuration.Harnesses, profiles = configuration.Profiles, languages = configuration.Languages });
        var manifest = new AiInstallationManifest(Revision(corpusPath), [.. installed.OrderBy(file => file.Destination, StringComparer.Ordinal)]);
        WriteJson(Path.Combine(projectPath, ManifestPath), manifest);
        return new(actions, conflicts);
    }

    /// <summary>Reads the selections offered by a Cratis AI corpus.</summary>
    /// <param name="corpusPath">The Cratis AI repository root.</param>
    /// <returns>The harnesses, profiles, and languages published by the corpus manifest.</returns>
    public static AiConfiguration Available(string corpusPath)
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpusPath, ".cratis", "ai", "manifest.json")));
        return new(ReadArray(manifest, "harnesses"), ReadArray(manifest, "profiles"), ReadArray(manifest, "languages"));
    }

    /// <summary>Returns configured and locally modified managed files without changing the project.</summary>
    /// <param name="projectPath">The consuming repository root.</param>
    /// <returns>The current installation state.</returns>
    public static AiStatus Status(string projectPath)
    {
        var configuration = ReadConfiguration(projectPath);
        var manifest = ReadManifest(projectPath);
        var modified = manifest.Files.Where(file =>
        {
            var path = Path.Combine(projectPath, ".cratis", "ai", file.Destination);
            return !File.Exists(path) || !string.Equals(Hash(File.ReadAllText(path)), file.Hash, StringComparison.Ordinal);
        }).Select(file => file.Destination).ToArray();
        return new(configuration, manifest.SourceRevision, modified);
    }

    /// <summary>Removes only unchanged managed files and Cratis-created harness settings.</summary>
    /// <param name="projectPath">The consuming repository root.</param>
    /// <param name="force">Whether modified managed files may be removed.</param>
    /// <returns>The changes made and files requiring user attention.</returns>
    public static SyncResult Uninstall(string projectPath, bool force = false)
    {
        var manifest = ReadManifest(projectPath);
        var actions = new List<string>();
        var conflicts = ModifiedFiles(projectPath, manifest);
        if (conflicts.Count > 0 && !force) return new([], conflicts);
        conflicts.Clear();
        foreach (var file in manifest.Files)
        {
            var path = Path.Combine(projectPath, ".cratis", "ai", file.Destination);
            if (!File.Exists(path)) continue;
            if (!force && !string.Equals(Hash(File.ReadAllText(path)), file.Hash, StringComparison.Ordinal))
            {
                conflicts.Add(file.Destination);
                continue;
            }

            File.Delete(path);
            actions.Add($"Removed {file.Destination}");
        }

        RemoveHarnessIntegrations(projectPath);
        var manifestPath = Path.Combine(projectPath, ManifestPath);
        if (File.Exists(manifestPath)) File.Delete(manifestPath);
        return new(actions, conflicts);
    }

    static IEnumerable<CorpusAsset> Resolve(string corpusPath, AiConfiguration configuration)
    {
        ValidateSelection(corpusPath, configuration);
        var catalogPath = Path.Combine(corpusPath, "distribution", "profile-catalog.json");
        using var catalog = JsonDocument.Parse(File.ReadAllText(catalogPath));
        var profiles = catalog.RootElement.EnumerateObject()
            .Where(property => string.Equals(property.Name, "publicProfiles", StringComparison.Ordinal) || string.Equals(property.Name, "engineeringProfiles", StringComparison.Ordinal))
            .SelectMany(property => property.Value.EnumerateArray())
            .ToArray();
        var selected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var profile in configuration.Profiles) SelectProfile(profile, selected, profiles);

        var skillNames = profiles.Where(profile => selected.Contains(profile.GetProperty("id").GetString()!))
            .SelectMany(AvailableTargets)
            .ToHashSet(StringComparer.Ordinal);
        var rulesRoot = Path.Combine(corpusPath, ".cratis", "ai", "rules");
        foreach (var rule in Directory.EnumerateFiles(rulesRoot, "*.md", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(rulesRoot, rule).Replace('\\', '/');
            yield return new(rule, $"rules/{relative}", $"rules/{relative}");
        }

        foreach (var skill in skillNames)
        {
            var directory = Path.Combine(corpusPath, ".cratis", "ai", "skills", skill);
            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(directory, file).Replace('\\', '/');
                yield return new(file, $"skills/{skill}/{relative}", $"skills/{skill}/{relative}");
            }
        }
    }

    static void ValidateSelection(string corpusPath, AiConfiguration configuration)
    {
        var available = Available(corpusPath);
        Validate("harness", configuration.Harnesses, available.Harnesses);
        Validate("profile", configuration.Profiles, available.Profiles);
        Validate("language", configuration.Languages, available.Languages);
    }

    static void Validate(string dimension, IEnumerable<string> selected, IEnumerable<string> available)
    {
        var known = available.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var value in selected)
        {
            if (!known.Contains(value)) throw new InvalidOperationException($"Cratis AI does not offer {dimension} '{value}'.");
        }
    }

    static IEnumerable<string> AvailableTargets(JsonElement profile) => profile.TryGetProperty("availableTargets", out var targets)
        ? targets.EnumerateArray().Select(target => target.GetString()!)
        : [];

    static void SelectProfile(string id, ISet<string> selected, IEnumerable<JsonElement> profiles)
    {
        if (!selected.Add(id)) return;
        var profile = profiles.FirstOrDefault(item => item.GetProperty("id").GetString() == id);
        if (profile.ValueKind == JsonValueKind.Undefined) throw new InvalidOperationException($"Unknown Cratis AI profile '{id}'.");
        if (!profile.TryGetProperty("composes", out var composes)) return;
        foreach (var child in composes.EnumerateArray()) SelectProfile(child.GetString()!, selected, profiles);
    }

    static void ConfigureHarnesses(string projectPath, IEnumerable<string> harnesses, IEnumerable<string> files)
    {
        var skills = files.Where(file => file.StartsWith("skills/", StringComparison.Ordinal))
            .Select(file => ".cratis/ai/" + file[..file.LastIndexOf('/')])
            .Distinct()
            .Order()
            .ToArray();
        if (harnesses.Contains("claude", StringComparer.OrdinalIgnoreCase)) CreateSkillLink(projectPath, ".claude", "../.cratis/ai/skills");
        if (harnesses.Contains("codex", StringComparer.OrdinalIgnoreCase)) CreateSkillLink(projectPath, ".agents", "../.cratis/ai/skills");
        if (harnesses.Contains("copilot", StringComparer.OrdinalIgnoreCase)) CreateSkillLink(projectPath, ".github", "../.cratis/ai/skills");
        if (harnesses.Contains("pi", StringComparer.OrdinalIgnoreCase))
        {
            CreateSkillLink(projectPath, ".pi", "../.cratis/ai/skills");
            WriteJson(Path.Combine(projectPath, ".pi", "settings.json"), new { skillPaths = skills, enableSkillCommands = true, cratisAiManaged = true });
        }
    }

    static void CreateSkillLink(string projectPath, string harnessDirectory, string target)
    {
        var directory = Path.Combine(projectPath, harnessDirectory);
        var link = Path.Combine(directory, "skills");
        if (Directory.Exists(link) || File.Exists(link)) return;
        Directory.CreateDirectory(directory);
        Directory.CreateSymbolicLink(link, target);
    }

    static void RemoveHarnessIntegrations(string projectPath)
    {
        foreach (var harness in new[] { ".claude", ".agents", ".github", ".pi" })
        {
            var link = new DirectoryInfo(Path.Combine(projectPath, harness, "skills"));
            if (string.Equals(link.LinkTarget, "../.cratis/ai/skills", StringComparison.Ordinal)) link.Delete();
        }

        var piSettings = Path.Combine(projectPath, ".pi", "settings.json");
        if (File.Exists(piSettings) && File.ReadAllText(piSettings).Contains("\"cratisAiManaged\": true", StringComparison.Ordinal)) File.Delete(piSettings);
    }

    static List<string> ModifiedFiles(string projectPath, AiInstallationManifest manifest) => [.. manifest.Files.Where(file =>
    {
        var path = Path.Combine(projectPath, ".cratis", "ai", file.Destination);
        return !File.Exists(path) || !string.Equals(Hash(File.ReadAllText(path)), file.Hash, StringComparison.Ordinal);
    }).Select(file => file.Destination)];

    static AiConfiguration ReadConfiguration(string projectPath)
    {
        var path = Path.Combine(projectPath, ConfigurationPath);
        if (!File.Exists(path)) throw new InvalidOperationException("No .cratis/ai.json exists. Run 'cratis ai install' first.");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return new(ReadArray(document, "harnesses"), ReadArray(document, "profiles"), ReadArray(document, "languages"));
    }

    static string[] ReadArray(JsonDocument document, string name) => [.. document.RootElement.GetProperty(name).EnumerateArray().Select(item => item.GetString()!)];

    static AiInstallationManifest ReadManifest(string projectPath)
    {
        var path = Path.Combine(projectPath, ManifestPath);
        return File.Exists(path) ? JsonSerializer.Deserialize<AiInstallationManifest>(File.ReadAllText(path))! : new("unknown", []);
    }

    static string AddMarker(string source, string content) => $"<!-- cratis-ai-managed: {source} -->\n{content}";
    static string Hash(string content) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    static string Revision(string corpusPath) => Directory.GetLastWriteTimeUtc(corpusPath).ToString("O");

    static void WriteJson(string path, object value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(value, _serializerOptions));
    }

    record CorpusAsset(string Path, string Destination, string Source);
}

/// <summary>The result of an install, update, or uninstall operation.</summary>
/// <param name="Actions">The files added, changed, or removed.</param>
/// <param name="Conflicts">Managed files that were changed locally.</param>
public sealed record SyncResult(IReadOnlyList<string> Actions, IReadOnlyList<string> Conflicts);

/// <summary>Read-only installation state.</summary>
/// <param name="Configuration">The configured selection.</param>
/// <param name="SourceRevision">The corpus revision used to synchronize.</param>
/// <param name="ModifiedFiles">Managed files changed or removed locally.</param>
public sealed record AiStatus(AiConfiguration Configuration, string SourceRevision, IReadOnlyList<string> ModifiedFiles);
