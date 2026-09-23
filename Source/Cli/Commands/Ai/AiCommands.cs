// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>Installs selected Cratis AI guidance into the current repository.</summary>
/// <remarks>
/// Writes the shared corpus to .cratis/ai, creates the native adapter for each selected harness pointing
/// at it, and records what it owns in .cratis/ai.manifest.json so a later update can tell its own files
/// from yours. User-owned paths are never overwritten.
/// </remarks>
[CliCommand(
    "install",
    "Install Cratis AI guidance into this repository, and record the choice in .cratis/ai.json.\n\n" +
    "One shared corpus is written to .cratis/ai, and every selected AI tool gets a native adapter linking into it, so all of them read the same guidance and a single update reaches all of them. A file you own is never replaced, and .cratis/ai.manifest.json records what Cratis installed so later updates can tell its files from yours. Commit .cratis/ai.json, .cratis/ai.manifest.json and the installed .cratis/ai.\n\n" +
    "--profiles is the decision that matters. It follows what you are building:\n" +
    "  cratis/application/*   you build an app on Cratis\n" +
    "  cratis/engineering/*   you build Cratis itself\n" +
    "  cratis/documentation   the repository holds docs\n\n" +
    "The first two are exclusive on purpose: an application repository given engineering profiles receives no slice guidance, and a framework repository given application profiles receives a manual that tells it not to apply.\n\n" +
    "Run install once per repository, then 'cratis ai update' to pick up newer guidance. Scaffolded from a Cratis template? The selection already exists, so run update instead.",
    Branch = typeof(AiBranch))]
[CliExample("ai", "install")]
[CliExample("ai", "install", "--profiles", "cratis/application/csharp", "--harnesses", "pi")]
[CliExample("ai", "install", "--profiles", "cratis/engineering/csharp", "--harnesses", "claude,pi", "--languages", "csharp")]
[CliExample("ai", "install", "--profiles", "cratis/documentation", "--harnesses", "pi", "--source", "../AI")]
[CliExample("ai", "install", "--profiles", "cratis/application/csharp", "--harnesses", "pi", "--dry-run")]
public sealed class AiInstallCommand : AsyncCommand<AiInstallSettings>
{
    public static int Write(SyncResult result, string format, bool dryRun = false)
    {
        OutputFormatter.WriteObject(format, new { actions = result.Actions, conflicts = result.Conflicts, unsupportedMcpServers = result.UnsupportedMcpServers ?? [], dryRun });
        if (result.Conflicts.Count == 0) return ExitCodes.Success;
        OutputFormatter.WriteError(format, "Cratis-managed AI files were modified or a user-owned path conflicts; no files were changed.", "Review the reported paths. Use --force only for managed corpus files; changed or foreign MCP entries are never overwritten.", ExitCodes.ValidationErrorCode);
        return ExitCodes.ValidationError;
    }

    public static string Source(AiSettings settings) => settings.Source ?? Environment.GetEnvironmentVariable("CRATIS_AI_SOURCE") ?? AiCorpusSource.Download();

    protected override Task<int> ExecuteAsync(CommandContext context, AiInstallSettings settings, CancellationToken cancellationToken)
    {
        var source = Source(settings);
        var available = AiCorpusSynchronizer.Available(source);
        var configuration = new AiConfiguration(
            Select(settings.Harnesses, "harnesses", available.Harnesses),
            Select(settings.Profiles, "profiles", available.Profiles),
            Select(settings.Languages, "languages", available.Languages, required: false));
        return Task.FromResult(Write(
            AiCorpusSynchronizer.Synchronize(Directory.GetCurrentDirectory(), source, configuration, settings.Force, settings.DryRun),
            settings.ResolveOutputFormat(),
            settings.DryRun));
    }

    static string[] Select(string? value, string label, IReadOnlyList<string> defaults, bool required = true)
    {
        if (!string.IsNullOrWhiteSpace(value)) return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // An omitted optional dimension constrains nothing, which is a meaningful selection rather than a
        // missing answer, so a non-interactive run does not have to state it.
        if (Console.IsInputRedirected && !required) return [];
        if (Console.IsInputRedirected) throw new InvalidOperationException($"--{label} is required when input is redirected.");
        var prompt = new MultiSelectionPrompt<string>()
            .Title($"Select {label}")
            .AddChoices(defaults);
        return [.. AnsiConsole.Prompt(prompt)];
    }
}

/// <summary>Synchronizes the configured Cratis AI corpus.</summary>
/// <remarks>
/// Reuses the harnesses, profiles and languages already recorded in .cratis/ai.json, so it takes no
/// selection options. Change the selection by running install again.
/// </remarks>
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
    protected override Task<int> ExecuteAsync(CommandContext context, AiSettings settings, CancellationToken cancellationToken)
    {
        var project = Directory.GetCurrentDirectory();
        var configuration = AiCorpusSynchronizer.Status(project).Configuration;
        return Task.FromResult(AiInstallCommand.Write(
            AiCorpusSynchronizer.Synchronize(project, AiInstallCommand.Source(settings), configuration, settings.Force, settings.DryRun),
            settings.ResolveOutputFormat(),
            settings.DryRun));
    }
}

/// <summary>Displays Cratis AI configuration and locally modified managed files.</summary>
/// <remarks>Read-only. Reports the installed revision, the revision available, and any managed file edited locally.</remarks>
[CliCommand(
    "status",
    "Show what is configured, installed, available and locally modified. Changes nothing.\n\n" +
    "Reports the selected profiles, harnesses and languages, the corpus revision installed here, the revision the source offers, whether an update is available, and any Cratis-managed file edited locally. Exits non-zero when local modifications exist, so it works as a CI check that guidance has not drifted.",
    Branch = typeof(AiBranch))]
[CliExample("ai", "status")]
[CliExample("ai", "status", "--output", "json")]
public sealed class AiStatusCommand : AsyncCommand<AiSettings>
{
    protected override Task<int> ExecuteAsync(CommandContext context, AiSettings settings, CancellationToken cancellationToken)
    {
        var status = AiCorpusSynchronizer.Status(Directory.GetCurrentDirectory());
        var availableSourceRevision = AiCorpusSynchronizer.Revision(AiInstallCommand.Source(settings));
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
[CliCommand(
    "uninstall",
    "Remove Cratis-managed AI content, preserving files you own.\n\n" +
    "Removes the managed files and the harness adapters Cratis created, using the manifest to decide what belongs to it. A managed file you edited is reported as a conflict and kept unless --force is given.",
    Branch = typeof(AiBranch))]
[CliExample("ai", "uninstall")]
[CliExample("ai", "uninstall", "--dry-run")]
public sealed class AiUninstallCommand : AsyncCommand<AiUninstallSettings>
{
    protected override Task<int> ExecuteAsync(CommandContext context, AiUninstallSettings settings, CancellationToken cancellationToken) =>
        Task.FromResult(AiInstallCommand.Write(
            AiCorpusSynchronizer.Uninstall(Directory.GetCurrentDirectory(), settings.Force, settings.DryRun),
            settings.ResolveOutputFormat(),
            settings.DryRun));
}
