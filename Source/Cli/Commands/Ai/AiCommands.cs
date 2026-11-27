// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>Installs selected Cratis AI guidance into the current repository.</summary>
/// <remarks>
/// Writes the shared corpus to .cratis/ai, creates the native adapter for each selected harness pointing
/// at it, and records what it owns in .cratis/ai.manifest.json so a later update can tell its own files
/// from yours. User-owned paths are never overwritten.
/// </remarks>
[CommandEffect(CommandEffect.Local)]
[CliCommand(
    "install",
    "Install Cratis AI guidance into this repository, and record the choice in .cratis/ai.json.\n\n" +
    "One shared corpus is written to .cratis/ai, and every selected AI tool gets a native adapter linking into it, so all of them read the same guidance and a single update reaches all of them. A file you own is never replaced, and .cratis/ai.manifest.json records what Cratis installed so later updates can tell its files from yours. Commit .cratis/ai.json, .cratis/ai.manifest.json and the installed .cratis/ai.\n\n" +
    "--profiles is the decision that matters. It follows what you are building:\n" +
    "  cratis/application   you build an application on Cratis\n" +
    "    cratis ai install --profiles cratis/application --harnesses pi --languages csharp,typescript\n" +
    "  cratis/engineering   you change a Cratis platform repository\n" +
    "    cratis ai install --profiles cratis/engineering --harnesses pi --languages csharp,typescript\n\n" +
    "The two are exclusive on purpose: an application repository given the engineering profile receives no slice guidance, and a platform repository given the application profile is told to structure a framework as an application. Narrower profiles from earlier releases stay selectable by id; 'cratis ai profiles list --all' lists them.\n\n" +
    "Run install once per repository, then 'cratis ai update' to pick up newer guidance. Scaffolded from a Cratis template? The selection already exists, so run update instead.",
    Branch = typeof(AiBranch))]
[CliExample("ai", "install")]
[CliExample("ai", "install", "--profiles", "cratis/application", "--harnesses", "pi", "--languages", "csharp,typescript")]
[CliExample("ai", "install", "--profiles", "cratis/engineering", "--harnesses", "claude,pi", "--languages", "csharp,typescript")]
[CliExample("ai", "install", "--profiles", "cratis/application", "--harnesses", "pi", "--source", "../AI")]
[CliExample("ai", "install", "--profiles", "cratis/application", "--harnesses", "pi", "--dry-run")]
public sealed class AiInstallCommand : AsyncCommand<AiInstallSettings>
{
    public static int Write(SyncResult result, string format, bool dryRun = false, IReadOnlyList<string>? profiles = null)
    {
        var warnings = new List<string>(result.Warnings ?? []);
        if (profiles?.Any(profile => profile == "cratis/codescene" || profile.StartsWith("cratis/codescene/", StringComparison.Ordinal)) is true)
        {
            warnings.Add("CodeScene: the CLI does not register the CodeScene MCP server. Register `cs-mcp` (`brew install cs-mcp`, or `npx -y @codescene/codehealth-mcp`) in your own user-level MCP configuration; do not add it to project files.");
        }
        OutputFormatter.WriteObject(format, new { actions = result.Actions, conflicts = result.Conflicts, unsupportedMcpServers = result.UnsupportedMcpServers ?? [], warnings, dryRun });
        if (result.Conflicts.Count == 0) return ExitCodes.Success;
        OutputFormatter.WriteError(format, "Cratis-managed AI files were modified, a user-owned path conflicts, or a managed file's mode cannot be changed; no files were changed.", "Review the reported paths. Use --force only for modified managed corpus files; changed or foreign MCP entries are never overwritten, and a file whose mode cannot be changed needs its owner to run the update, or a file system that keeps file modes.", ExitCodes.ValidationErrorCode);
        return ExitCodes.ValidationError;
    }

    /// <summary>Resolves the corpus from <c language="csharp">--source</c>, <c language="csharp">CRATIS_AI_SOURCE</c> or a download.</summary>
    /// <param name="settings">The command settings.</param>
    /// <returns>The corpus. Dispose it so a downloaded copy is deleted; a supplied source never is.</returns>
    public static AiCorpus Corpus(AiSettings settings) =>
        AiCorpusSource.Resolve(settings.Source ?? Environment.GetEnvironmentVariable("CRATIS_AI_SOURCE"));

    public override Task<int> ExecuteAsync(CommandContext context, AiInstallSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var missing = MissingSelection(settings, Console.IsInputRedirected);
        if (missing is not null) return Task.FromResult(InvalidUsage(format, missing));

        using var corpus = Corpus(settings);
        var source = corpus.Path;
        var available = AiCorpusSynchronizer.Available(source);
        var configuration = new AiConfiguration(
            Select(settings.Harnesses, "harnesses", available.Harnesses),
            SelectProfiles(settings.Profiles, source, available.Profiles),
            Select(settings.Languages, "languages", available.Languages));
        var unoffered = Unoffered(configuration, available);
        if (unoffered is not null) return Task.FromResult(InvalidUsage(format, unoffered));
        return Task.FromResult(Write(
            AiCorpusSynchronizer.Synchronize(Directory.GetCurrentDirectory(), source, configuration, settings.Force, settings.DryRun),
            format,
            settings.DryRun,
            configuration.Profiles));
    }

    /// <summary>Names the required selection a run without a terminal to prompt on leaves out, if any.</summary>
    /// <param name="settings">The command settings.</param>
    /// <param name="inputRedirected">Whether input is redirected, so nothing can be prompted for.</param>
    /// <returns>The problem to report as invalid usage, or <see langword="null"/> when nothing required is missing.</returns>
    internal static string? MissingSelection(AiInstallSettings settings, bool inputRedirected)
    {
        if (!inputRedirected) return null;
        if (string.IsNullOrWhiteSpace(settings.Harnesses)) return "--harnesses is required when input is redirected.";
        return string.IsNullOrWhiteSpace(settings.Profiles) ? "--profiles is required when input is redirected." : null;
    }

    /// <summary>Names the first selected value the corpus does not offer, if any.</summary>
    /// <param name="selected">The selection.</param>
    /// <param name="available">What the corpus offers.</param>
    /// <returns>The problem to report as invalid usage, or <see langword="null"/> when the corpus offers every value.</returns>
    internal static string? Unoffered(AiConfiguration selected, AiConfiguration available) =>
        Unoffered("harness", selected.Harnesses, available.Harnesses) ??
        Unoffered("profile", selected.Profiles, available.Profiles) ??
        Unoffered("language", selected.Languages, available.Languages);

    static int InvalidUsage(string format, string problem)
    {
        OutputFormatter.WriteError(format, problem, "Run 'cratis ai install' interactively to choose from what the corpus offers.", ExitCodes.ValidationErrorCode);
        return ExitCodes.ValidationError;
    }

    static string? Unoffered(string dimension, IEnumerable<string> selected, IEnumerable<string> available)
    {
        var known = available.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var value = selected.FirstOrDefault(candidate => !known.Contains(candidate));
        return value is null ? null : $"Cratis AI does not offer {dimension} '{value}'.";
    }

    static string[] SelectProfiles(string? value, string corpusPath, IReadOnlyList<string> offered)
    {
        // Nothing is prompted for when the profiles were given or there is no terminal, so the catalog need not be read.
        if (!string.IsNullOrWhiteSpace(value) || Console.IsInputRedirected) return Select(value, "profiles", offered);
        var featured = AiCorpusProfiles.Listed(corpusPath).Where(profile => profile.Featured).ToDictionary(profile => profile.Id, StringComparer.Ordinal);
        if (featured.Count == 0) return Select(value, "profiles", offered);
        return Select(
            value,
            "profiles (every other profile stays selectable with --profiles)",
            [.. featured.Keys],
            id => Markup.Escape(featured[id].Description is { } description ? $"{id} - {description}" : id));
    }

    static string[] Select(string? value, string label, IReadOnlyList<string> defaults, Func<string, string>? display = null)
    {
        if (!string.IsNullOrWhiteSpace(value)) return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Required selections were checked before the corpus was resolved, so only an omitted optional dimension
        // reaches here without a terminal. It constrains nothing, which is a meaningful selection rather than a
        // missing answer, so a non-interactive run does not have to state it.
        if (Console.IsInputRedirected) return [];
        var prompt = new MultiSelectionPrompt<string>()
            .Title($"Select {label}")
            .AddChoices(defaults);
        if (display is not null) prompt.UseConverter(display);
        return [.. AnsiConsole.Prompt(prompt)];
    }
}

/// <summary>Synchronizes the configured Cratis AI corpus.</summary>
/// <remarks>
/// Reuses the harnesses, profiles and languages already recorded in .cratis/ai.json, so it takes no
/// selection options. Change the selection by running install again.
/// </remarks>
[CommandEffect(CommandEffect.Local)]
[CliCommand(
    "update",
    "Bring Cratis-managed AI content up to the current corpus.\n\n" +
    "Reuses the selection already recorded in .cratis/ai.json, so it takes no profile, harness or language options; run install again to change the selection. Only files the manifest records as Cratis-managed are touched, and a file you edited is reported and left alone unless --force is given.",
    Branch = typeof(AiBranch))]
[CliExample("ai", "update")]
[CliExample("ai", "update", "--dry-run")]
[CliExample("ai", "update", "--source", "../AI")]
public sealed class AiUpdateCommand : AsyncCommand<AiSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, AiSettings settings, CancellationToken cancellationToken)
    {
        var project = Directory.GetCurrentDirectory();
        var configuration = AiCorpusSynchronizer.Status(project).Configuration;
        using var corpus = AiInstallCommand.Corpus(settings);
        return Task.FromResult(AiInstallCommand.Write(
            AiCorpusSynchronizer.Synchronize(project, corpus.Path, configuration, settings.Force, settings.DryRun),
            settings.ResolveOutputFormat(),
            settings.DryRun,
            configuration.Profiles));
    }
}

/// <summary>Displays Cratis AI configuration and locally modified managed files.</summary>
/// <remarks>Read-only. Reports the installed revision, the revision available, and any managed file edited locally.</remarks>
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand(
    "status",
    "Show what is configured, installed, available and locally modified. Changes nothing.\n\n" +
    "Reports the selected profiles, harnesses and languages, the corpus revision installed here, the revision the source offers, whether an update is available, and any Cratis-managed file edited locally. Exits non-zero when local modifications exist, so it works as a CI check that guidance has not drifted.",
    Branch = typeof(AiBranch))]
[CliExample("ai", "status")]
[CliExample("ai", "status", "--output", "json")]
public sealed class AiStatusCommand : AsyncCommand<AiSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, AiSettings settings, CancellationToken cancellationToken)
    {
        var status = AiCorpusSynchronizer.Status(Directory.GetCurrentDirectory());
        using var corpus = AiInstallCommand.Corpus(settings);
        var availableSourceRevision = AiCorpusSynchronizer.Revision(corpus.Path);
        OutputFormatter.WriteObject(settings.ResolveOutputFormat(), new
        {
            harnesses = status.Configuration.Harnesses,
            profiles = status.Configuration.Profiles,
            languages = status.Configuration.Languages,
            sourceRevision = status.SourceRevision,
            availableSourceRevision,
            updateAvailable = !string.Equals(status.SourceRevision, availableSourceRevision, StringComparison.Ordinal),
            modifiedFiles = status.ModifiedFiles,
            mcpConfiguration = status.Configuration.McpServers,
            mcpServers = (status.McpServers ?? []).Select(server => new { harness = server.Harness, path = server.Path, id = server.Id }),
            mcpExtensions = status.McpExtensions ?? [],
            unsupportedMcpServers = status.UnsupportedMcpServers ?? [],
        });
        return Task.FromResult(status.ModifiedFiles.Count == 0 ? ExitCodes.Success : ExitCodes.ValidationError);
    }
}

/// <summary>Removes unchanged Cratis-managed AI content while preserving user files.</summary>
[CommandEffect(CommandEffect.Local)]
[CliCommand(
    "uninstall",
    "Remove Cratis-managed AI content, preserving files you own.\n\n" +
    "Removes the managed files and the harness adapters Cratis created, using the manifest to decide what belongs to it. A managed file you edited is reported as a conflict and kept unless --force is given.",
    Branch = typeof(AiBranch))]
[CliExample("ai", "uninstall")]
[CliExample("ai", "uninstall", "--dry-run")]
public sealed class AiUninstallCommand : AsyncCommand<AiUninstallSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, AiUninstallSettings settings, CancellationToken cancellationToken) =>
        Task.FromResult(AiInstallCommand.Write(
            AiCorpusSynchronizer.Uninstall(Directory.GetCurrentDirectory(), settings.Force, settings.DryRun),
            settings.ResolveOutputFormat(),
            settings.DryRun));
}
