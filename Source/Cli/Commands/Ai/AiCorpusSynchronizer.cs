// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Ai;

/// <summary>Synchronizes only the files and harness integrations recorded as Cratis-owned in a project.</summary>
public static class AiCorpusSynchronizer
{
    const string ConfigurationPath = ".cratis/ai.json";
    const string ManifestPath = ".cratis/ai.manifest.json";
    const string ManagedRoot = ".cratis/ai";
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
        var integrationPlans = PlanHarnessIntegrations(configuration.Harnesses, desired.Keys);
        var previousIntegrations = (previous.Integrations ?? []).ToDictionary(integration => integration.Path, StringComparer.Ordinal);
        var modified = ModifiedFiles(projectPath, previous);
        modified.AddRange(ModifiedIntegrations(projectPath, previous));

        var unknownCollisions = desired.Keys
            .Where(destination => PathExists(Path.Combine(projectPath, ManagedRoot, destination)) && !previous.Files.Any(file => file.Destination == destination))
            .ToList();
        unknownCollisions.AddRange(integrationPlans.Values
            .Where(plan => !plan.PreserveExisting && PathExists(Path.Combine(projectPath, plan.Path)) && !previousIntegrations.ContainsKey(plan.Path) && !IntegrationMatches(projectPath, plan))
            .Select(plan => plan.Path));

        if (unknownCollisions.Count > 0) return new([], [.. unknownCollisions.Distinct(StringComparer.Ordinal).Order()]);
        if (modified.Count > 0 && !force) return new([], [.. modified.Distinct(StringComparer.Ordinal).Order()]);

        var actions = new List<string>();
        var installed = new List<AiManagedFile>();
        foreach (var asset in desired.Values.OrderBy(asset => asset.Destination, StringComparer.Ordinal))
        {
            var destination = Path.Combine(projectPath, ManagedRoot, asset.Destination);
            var content = AddMarker(asset.Source, asset.Path, File.ReadAllText(asset.Path));
            var hash = Hash(content);
            var existed = File.Exists(destination);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.WriteAllText(destination, content);
            installed.Add(new(asset.Source, asset.Destination, hash));
            actions.Add(existed ? $"Updated {asset.Destination}" : $"Added {asset.Destination}");
        }

        foreach (var existing in previous.Files.Where(file => !desired.ContainsKey(file.Destination)))
        {
            var destination = Path.Combine(projectPath, ManagedRoot, existing.Destination);
            if (!File.Exists(destination)) continue;
            File.Delete(destination);
            actions.Add($"Removed {existing.Destination}");
        }

        foreach (var existing in previousIntegrations.Values.Where(integration => !integrationPlans.ContainsKey(integration.Path)))
        {
            DeleteIntegration(projectPath, existing);
            actions.Add($"Removed {existing.Path}");
        }

        var installedIntegrations = new List<AiManagedIntegration>();
        foreach (var plan in integrationPlans.Values.OrderBy(plan => plan.Path, StringComparer.Ordinal))
        {
            if (!previousIntegrations.ContainsKey(plan.Path) && plan.PreserveExisting && PathExists(Path.Combine(projectPath, plan.Path))) continue;
            if (!previousIntegrations.ContainsKey(plan.Path) && IntegrationMatches(projectPath, plan)) continue;
            if (!IntegrationMatches(projectPath, plan))
            {
                if (PathExists(Path.Combine(projectPath, plan.Path))) DeleteIntegration(projectPath, plan);
                CreateIntegration(projectPath, plan);
                actions.Add($"Configured {plan.Path}");
            }
            installedIntegrations.Add(plan);
        }

        WriteJson(Path.Combine(projectPath, ConfigurationPath), new { schemaVersion = AiConfiguration.SchemaVersion, harnesses = configuration.Harnesses, profiles = configuration.Profiles, languages = configuration.Languages });
        var manifest = new AiInstallationManifest(
            Revision(corpusPath),
            [.. installed.OrderBy(file => file.Destination, StringComparer.Ordinal)],
            [.. installedIntegrations.OrderBy(integration => integration.Path, StringComparer.Ordinal)]);
        WriteJson(Path.Combine(projectPath, ManifestPath), manifest);
        return new(actions, []);
    }

    /// <summary>Reads the selections offered by a Cratis AI corpus.</summary>
    /// <param name="corpusPath">The Cratis AI repository root.</param>
    /// <returns>The harnesses, profiles, and languages published by the corpus manifest.</returns>
    public static AiConfiguration Available(string corpusPath)
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpusPath, ManagedRoot, "manifest.json")));
        return new(ReadArray(manifest, "harnesses"), ReadArray(manifest, "profiles"), ReadArray(manifest, "languages"));
    }

    /// <summary>Returns configured and locally modified managed files without changing the project.</summary>
    /// <param name="projectPath">The consuming repository root.</param>
    /// <returns>The current installation state.</returns>
    public static AiStatus Status(string projectPath)
    {
        var configuration = ReadConfiguration(projectPath);
        var manifest = ReadManifest(projectPath);
        var modified = ModifiedFiles(projectPath, manifest);
        modified.AddRange(ModifiedIntegrations(projectPath, manifest));
        return new(configuration, manifest.SourceRevision, [.. modified.Distinct(StringComparer.Ordinal).Order()]);
    }

    /// <summary>Removes only unchanged managed files and Cratis-created harness integrations.</summary>
    /// <param name="projectPath">The consuming repository root.</param>
    /// <param name="force">Whether modified managed files may be removed.</param>
    /// <returns>The changes made and files requiring user attention.</returns>
    public static SyncResult Uninstall(string projectPath, bool force = false)
    {
        var manifest = ReadManifest(projectPath);
        var modified = ModifiedFiles(projectPath, manifest);
        modified.AddRange(ModifiedIntegrations(projectPath, manifest));
        if (modified.Count > 0 && !force) return new([], [.. modified.Distinct(StringComparer.Ordinal).Order()]);

        var actions = new List<string>();
        foreach (var file in manifest.Files)
        {
            var path = Path.Combine(projectPath, ManagedRoot, file.Destination);
            if (!File.Exists(path)) continue;
            File.Delete(path);
            actions.Add($"Removed {file.Destination}");
        }

        foreach (var integration in manifest.Integrations ?? [])
        {
            if (!PathExists(Path.Combine(projectPath, integration.Path))) continue;
            DeleteIntegration(projectPath, integration);
            actions.Add($"Removed {integration.Path}");
        }

        var manifestPath = Path.Combine(projectPath, ManifestPath);
        if (File.Exists(manifestPath)) File.Delete(manifestPath);
        return new(actions, []);
    }

    /// <summary>Gets the immutable Git revision for a corpus checkout, or its last-write timestamp when it is not a Git checkout.</summary>
    /// <param name="corpusPath">The Cratis AI repository root.</param>
    /// <returns>The source revision identity.</returns>
    public static string Revision(string corpusPath)
    {
        using var process = Process.Start(new ProcessStartInfo("git", $"-C \"{corpusPath}\" rev-parse HEAD")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        });
        if (process is not null)
        {
            var revision = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            if (process.ExitCode == 0 && revision.Length > 0) return revision;
        }
        return Directory.GetLastWriteTimeUtc(corpusPath).ToString("O");
    }

    static IEnumerable<CorpusAsset> Resolve(string corpusPath, AiConfiguration configuration)
    {
        ValidateSelection(corpusPath, configuration);
        var catalogPath = Path.Combine(corpusPath, ManagedRoot, "profile-catalog.json");
        using var catalog = JsonDocument.Parse(File.ReadAllText(catalogPath));
        var profiles = catalog.RootElement.EnumerateObject()
            .Where(property => string.Equals(property.Name, "publicProfiles", StringComparison.Ordinal) || string.Equals(property.Name, "engineeringProfiles", StringComparison.Ordinal))
            .SelectMany(property => property.Value.EnumerateArray())
            .ToArray();
        var selected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var profile in configuration.Profiles) SelectProfile(profile, selected, profiles, configuration.Languages, explicitSelection: true);

        var skillNames = profiles.Where(profile => selected.Contains(profile.GetProperty("id").GetString()!))
            .SelectMany(AvailableTargets)
            .ToHashSet(StringComparer.Ordinal);
        var corpusRoot = Path.Combine(corpusPath, ManagedRoot);
        foreach (var category in new[] { "rules", "agents", "prompts", "hooks" })
        {
            foreach (var asset in AssetsUnder(corpusRoot, category)) yield return asset;
        }

        foreach (var skill in skillNames)
        {
            foreach (var asset in AssetsUnder(corpusRoot, $"skills/{skill}", excludeVerification: true)) yield return asset;
        }

        foreach (var harness in configuration.Harnesses)
        {
            foreach (var asset in AssetsUnder(corpusRoot, $"harnesses/{harness}")) yield return asset;
        }
    }

    static IEnumerable<CorpusAsset> AssetsUnder(string corpusRoot, string relativeRoot, bool excludeVerification = false)
    {
        var root = Path.Combine(corpusRoot, relativeRoot.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(root)) yield break;
        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(corpusRoot, path).Replace('\\', '/');
            if (excludeVerification && (relative.EndsWith("/verification.json", StringComparison.Ordinal) || relative.Contains("/evals/", StringComparison.Ordinal))) continue;
            yield return new(path, relative, relative);
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

    static void SelectProfile(string id, ISet<string> selected, IReadOnlyList<JsonElement> profiles, IReadOnlyList<string> languages, bool explicitSelection)
    {
        var profile = profiles.FirstOrDefault(item => item.GetProperty("id").GetString() == id);
        if (profile.ValueKind == JsonValueKind.Undefined) throw new InvalidOperationException($"Unknown Cratis AI profile '{id}'.");
        if (!explicitSelection && !SupportsAnyLanguage(profile, languages)) return;
        if (!selected.Add(id) || !profile.TryGetProperty("composes", out var composes)) return;
        foreach (var child in composes.EnumerateArray()) SelectProfile(child.GetString()!, selected, profiles, languages, explicitSelection: false);
    }

    static bool SupportsAnyLanguage(JsonElement profile, IReadOnlyList<string> languages)
    {
        if (!profile.TryGetProperty("languages", out var supported)) return true;
        var values = supported.EnumerateArray().Select(language => language.GetString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return values.Contains("language-agnostic") || languages.Any(values.Contains);
    }

    static Dictionary<string, AiManagedIntegration> PlanHarnessIntegrations(IEnumerable<string> harnesses, IEnumerable<string> files)
    {
        var plans = new Dictionary<string, AiManagedIntegration>(StringComparer.Ordinal);
        var selected = harnesses.ToHashSet(StringComparer.OrdinalIgnoreCase);
        void Add(string path, string target, bool isDirectory, bool preserveExisting = false) => plans.TryAdd(path, new(path, target, isDirectory, preserveExisting));
        void AddRootInstructions() => Add("AGENTS.md", ".cratis/ai/rules/general.md", false, preserveExisting: true);
        void AddCommands(string directory)
        {
            foreach (var prompt in files.Where(file => file.StartsWith("prompts/", StringComparison.Ordinal) && file.EndsWith(".prompt.md", StringComparison.Ordinal)))
            {
                var name = Path.GetFileName(prompt)[..^".prompt.md".Length];
                Add($"{directory}/{name}.md", $"../../.cratis/ai/{prompt}", false);
            }
        }
        void AddAgents(string directory, string suffix)
        {
            foreach (var agent in files.Where(file => file.StartsWith("agents/", StringComparison.Ordinal) && file.EndsWith(".md", StringComparison.Ordinal)))
            {
                var name = Path.GetFileNameWithoutExtension(agent);
                Add($"{directory}/{name}{suffix}", $"../../.cratis/ai/{agent}", false);
            }
        }

        if (selected.Contains("claude"))
        {
            Add(".claude/CLAUDE.md", "../.cratis/ai/rules/general.md", false);
            Add(".claude/agents", "../.cratis/ai/agents", true);
            Add(".claude/hooks", "../.cratis/ai/hooks", true);
            Add(".claude/prompts", "../.cratis/ai/prompts", true);
            Add(".claude/rules", "../.cratis/ai/rules", true);
            Add(".claude/settings.json", "../.cratis/ai/hooks/settings.template.json", false);
            Add(".claude/skills", "../.cratis/ai/skills", true);
            AddCommands(".claude/commands");
        }
        if (selected.Contains("codex"))
        {
            AddRootInstructions();
            Add(".agents/skills", "../.cratis/ai/skills", true);
        }
        if (selected.Contains("copilot"))
        {
            Add(".github/copilot-instructions.md", "../.cratis/ai/rules/general.md", false);
            Add(".github/instructions", "../.cratis/ai/rules", true);
            Add(".github/prompts", "../.cratis/ai/prompts", true);
            Add(".github/skills", "../.cratis/ai/skills", true);
            AddAgents(".github/agents", ".agent.md");
        }
        if (selected.Contains("pi"))
        {
            AddRootInstructions();
            Add(".pi/agents", "../.cratis/ai/agents", true);
            Add(".pi/extensions", "../.cratis/ai/harnesses/pi/extensions", true);
            Add(".pi/skills", "../.cratis/ai/skills", true);
            AddCommands(".pi/prompts");
        }
        if (selected.Contains("cursor"))
        {
            Add(".cursor/agents", "../.cratis/ai/agents", true);
            Add(".cursor/rules", "../.cratis/ai/harnesses/cursor/rules", true);
            Add(".cursor/skills", "../.cratis/ai/skills", true);
            AddCommands(".cursor/commands");
        }
        if (selected.Contains("opencode"))
        {
            AddRootInstructions();
            Add(".opencode/agents", "../.cratis/ai/agents", true);
            Add(".opencode/skills", "../.cratis/ai/skills", true);
            AddCommands(".opencode/commands");
        }
        return plans;
    }

    static void CreateIntegration(string projectPath, AiManagedIntegration integration)
    {
        var path = Path.Combine(projectPath, integration.Path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (integration.IsDirectory) Directory.CreateSymbolicLink(path, integration.Target);
        else File.CreateSymbolicLink(path, integration.Target);
    }

    static void DeleteIntegration(string projectPath, AiManagedIntegration integration)
    {
        var path = Path.Combine(projectPath, integration.Path);
        if (new DirectoryInfo(path).LinkTarget is not null) Directory.Delete(path);
        else File.Delete(path);
    }

    static bool IntegrationMatches(string projectPath, AiManagedIntegration integration)
    {
        var path = Path.Combine(projectPath, integration.Path);
        var target = integration.IsDirectory ? new DirectoryInfo(path).LinkTarget : new FileInfo(path).LinkTarget;
        return string.Equals(target, integration.Target, StringComparison.Ordinal);
    }

    static List<string> ModifiedFiles(string projectPath, AiInstallationManifest manifest) => [.. manifest.Files.Where(file =>
    {
        var path = Path.Combine(projectPath, ManagedRoot, file.Destination);
        return !File.Exists(path) || !string.Equals(Hash(File.ReadAllText(path)), file.Hash, StringComparison.Ordinal);
    }).Select(file => file.Destination)];

    static List<string> ModifiedIntegrations(string projectPath, AiInstallationManifest manifest) => [.. (manifest.Integrations ?? [])
        .Where(integration => !IntegrationMatches(projectPath, integration))
        .Select(integration => integration.Path)];

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
        return File.Exists(path) ? JsonSerializer.Deserialize<AiInstallationManifest>(File.ReadAllText(path))! : new("unknown", [], []);
    }

    static string AddMarker(string source, string path, string content)
    {
        var marker = $"cratis-ai-managed: {source}";
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (string.Equals(extension, ".md", StringComparison.Ordinal) || string.Equals(extension, ".mdc", StringComparison.Ordinal))
        {
            if (!content.StartsWith("---\n", StringComparison.Ordinal)) return $"<!-- {marker} -->\n{content}";
            var frontmatterEnd = content.IndexOf("\n---\n", 4, StringComparison.Ordinal);
            return frontmatterEnd < 0 ? $"<!-- {marker} -->\n{content}" : content.Insert(frontmatterEnd + 5, $"<!-- {marker} -->\n");
        }
        if (string.Equals(extension, ".html", StringComparison.Ordinal) || string.Equals(extension, ".htm", StringComparison.Ordinal)) return $"<!-- {marker} -->\n{content}";
        if (extension == ".json")
        {
            if (JsonNode.Parse(content) is JsonObject json)
            {
                json["$cratisAiManaged"] = source;
                return json.ToJsonString(_serializerOptions);
            }
            return content;
        }
        if (string.Equals(extension, ".sh", StringComparison.Ordinal) ||
            string.Equals(extension, ".py", StringComparison.Ordinal) ||
            string.Equals(extension, ".yml", StringComparison.Ordinal) ||
            string.Equals(extension, ".yaml", StringComparison.Ordinal))
        {
            if (content.StartsWith("#!", StringComparison.Ordinal))
            {
                var lineEnd = content.IndexOf('\n');
                if (lineEnd >= 0) return content.Insert(lineEnd + 1, $"# {marker}\n");
            }
            return $"# {marker}\n{content}";
        }
        return $"# {marker}\n{content}";
    }

    static string Hash(string content) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

    static bool PathExists(string path)
    {
        try
        {
            _ = File.GetAttributes(path);
            return true;
        }
        catch (FileNotFoundException)
        {
            return new FileInfo(path).LinkTarget is not null || new DirectoryInfo(path).LinkTarget is not null;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
    }

    static void WriteJson(string path, object value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(value, _serializerOptions));
    }

    sealed record CorpusAsset(string Path, string Destination, string Source);
}

/// <summary>The result of an install, update, or uninstall operation.</summary>
/// <param name="Actions">The files added, changed, or removed.</param>
/// <param name="Conflicts">Managed files that were changed locally or user-owned paths that would be overwritten.</param>
public sealed record SyncResult(IReadOnlyList<string> Actions, IReadOnlyList<string> Conflicts);

/// <summary>Read-only installation state.</summary>
/// <param name="Configuration">The configured selection.</param>
/// <param name="SourceRevision">The corpus revision used to synchronize.</param>
/// <param name="ModifiedFiles">Managed files or integrations changed or removed locally.</param>
public sealed record AiStatus(AiConfiguration Configuration, string SourceRevision, IReadOnlyList<string> ModifiedFiles);
