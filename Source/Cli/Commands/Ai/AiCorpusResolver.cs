// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.Commands.Ai;

/// <summary>Resolves which corpus files a configuration installs.</summary>
static class AiCorpusResolver
{
    static readonly string[] _categories = ["agents", "prompts", "hooks"];

    internal static IEnumerable<AiCorpusAsset> Resolve(string corpusPath, AiConfiguration configuration)
    {
        var selected = AiCorpusProfiles.Selected(corpusPath, configuration);
        var catalogPath = Path.Combine(corpusPath, AiCorpusSynchronizer.ManagedRoot, "profile-catalog.json");
        using var catalog = JsonDocument.Parse(File.ReadAllText(catalogPath));
        var profiles = AiCorpusProfiles.CatalogProfiles(catalog);
        foreach (var asset in DescriptorAssets(corpusPath, catalogPath)) yield return asset;

        var skillNames = profiles.Where(profile => selected.Contains(profile.GetProperty("id").GetString()!))
            .SelectMany(AiCorpusProfiles.AvailableTargets)
            .ToHashSet(StringComparer.Ordinal);
        var corpusRoot = Path.Combine(corpusPath, AiCorpusSynchronizer.ManagedRoot);
        var assets = RuleAssets(corpusRoot, configuration, selected)
            .Concat(CategoryAssets(corpusRoot))
            .Concat(SkillAssets(corpusRoot, skillNames))
            .Concat(HarnessAssets(corpusRoot, configuration));
        foreach (var asset in assets) yield return asset;
    }

    static IEnumerable<AiCorpusAsset> DescriptorAssets(string corpusPath, string catalogPath)
    {
        var descriptor = AiProjectPaths.Within(AiProjectPaths.PhysicalRoot(corpusPath), AiMcpDescriptor.RelativePath);
        if (!File.Exists(descriptor)) yield break;
        yield return new(descriptor, "mcp-servers.json", "mcp-servers.json");
        yield return new(catalogPath, "profile-catalog.json", "profile-catalog.json");
    }

    static IEnumerable<AiCorpusAsset> RuleAssets(string corpusRoot, AiConfiguration configuration, HashSet<string> selected) =>
        AssetsUnder(corpusRoot, "rules").Where(rule => RuleMatchesConfiguration(rule.Path, configuration, selected));

    static IEnumerable<AiCorpusAsset> CategoryAssets(string corpusRoot) =>
        _categories.SelectMany(category => AssetsUnder(corpusRoot, category));

    static IEnumerable<AiCorpusAsset> SkillAssets(string corpusRoot, IEnumerable<string> skillNames) =>
        skillNames.SelectMany(skill => AssetsUnder(corpusRoot, $"skills/{skill}", excludeVerification: true));

    static IEnumerable<AiCorpusAsset> HarnessAssets(string corpusRoot, AiConfiguration configuration) =>
        configuration.Harnesses.SelectMany(harness => AssetsUnder(corpusRoot, $"harnesses/{harness}"));

    static IEnumerable<AiCorpusAsset> AssetsUnder(string corpusRoot, string relativeRoot, bool excludeVerification = false)
    {
        var root = Path.Combine(corpusRoot, relativeRoot.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(root)) yield break;
        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(corpusRoot, path).Replace('\\', '/');
            if (excludeVerification && IsVerificationAsset(relative)) continue;
            yield return new(path, relative, relative);
        }
    }

    static bool IsVerificationAsset(string relative) =>
        relative.EndsWith("/verification.json", StringComparison.Ordinal) || relative.Contains("/evals/", StringComparison.Ordinal);

    static bool RuleMatchesConfiguration(string path, AiConfiguration configuration, HashSet<string> selectedProfiles)
    {
        var content = File.ReadAllText(path);
        var frontmatterEnd = content.StartsWith("---\n", StringComparison.Ordinal) ? content.IndexOf("\n---\n", 4, StringComparison.Ordinal) : -1;
        var frontmatter = frontmatterEnd < 0 ? string.Empty : content[4..frontmatterEnd];
        if (!ProfileAllowsRule(FrontmatterValue(frontmatter, "profile"), configuration, selectedProfiles)) return false;

        return LanguageAllowsRule(path, FrontmatterValue(frontmatter, "applyTo") ?? string.Empty, configuration, selectedProfiles);
    }

    static bool ProfileAllowsRule(string? profile, AiConfiguration configuration, HashSet<string> selectedProfiles)
    {
        if (string.Equals(profile, "application", StringComparison.OrdinalIgnoreCase)) return HasSelectedProfile(selectedProfiles, "cratis/application");
        if (string.Equals(profile, "framework", StringComparison.OrdinalIgnoreCase)) return HasSelectedProfile(selectedProfiles, "cratis/engineering");
        if (profile?.StartsWith("cratis/", StringComparison.Ordinal) is not true) return true;

        return configuration.Profiles.Any(candidate => candidate == profile || candidate.StartsWith($"{profile}/", StringComparison.Ordinal));
    }

    static bool HasSelectedProfile(HashSet<string> selectedProfiles, string prefix) =>
        selectedProfiles.Any(candidate => candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    static bool LanguageAllowsRule(string path, string applyTo, AiConfiguration configuration, HashSet<string> selectedProfiles)
    {
        var requirements = LanguageRequirements(path, applyTo);
        if (requirements == (false, false, false)) return true;

        var hasCSharp = configuration.Languages.Contains("csharp", StringComparer.OrdinalIgnoreCase);
        var hasTypeScript = configuration.Languages.Contains("typescript", StringComparer.OrdinalIgnoreCase);
        var hasDocumentation = selectedProfiles.Contains("cratis/documentation");
        return (requirements.CSharp && hasCSharp) || (requirements.TypeScript && hasTypeScript) || (requirements.Documentation && hasDocumentation);
    }

    static (bool CSharp, bool TypeScript, bool Documentation) LanguageRequirements(string path, string applyTo)
    {
        var needsCSharp = applyTo.Contains(".cs", StringComparison.OrdinalIgnoreCase);
        var needsTypeScript = applyTo.Contains(".ts", StringComparison.OrdinalIgnoreCase);
        var needsDocumentation = applyTo.Contains("md", StringComparison.OrdinalIgnoreCase);
        if (string.Equals(Path.GetFileName(path), "rtk.md", StringComparison.OrdinalIgnoreCase)) needsTypeScript = true;
        return (needsCSharp, needsTypeScript, needsDocumentation);
    }

    static string? FrontmatterValue(string frontmatter, string name)
    {
        var prefix = $"{name}:";
        var line = frontmatter.Split('\n').FirstOrDefault(candidate => candidate.StartsWith(prefix, StringComparison.Ordinal));
        return line?[prefix.Length..].Trim().Trim('"');
    }
}
