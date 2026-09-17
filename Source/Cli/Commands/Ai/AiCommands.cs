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
    "Install Cratis AI rules, skills and harness adapters into this repository. Writes the shared corpus to .cratis/ai, points each selected AI tool at it, and records what it owns so updates never overwrite your edits. Run it once per repository; use 'update' afterwards",
    Branch = typeof(AiBranch))]
[CliExample("ai", "install")]
[CliExample("ai", "install", "--harnesses", "claude,codex,copilot,cursor,opencode,pi", "--profiles", "cratis/application/csharp,cratis/documentation", "--languages", "csharp,typescript")]
[CliExample("ai", "install", "--profiles", "cratis/engineering/csharp", "--harnesses", "pi")]
[CliExample("ai", "install", "--source", "../AI")]
public sealed class AiInstallCommand : AsyncCommand<AiInstallSettings>
{
    public static int Write(SyncResult result, string format)
    {
        OutputFormatter.WriteObject(format, new { actions = result.Actions, conflicts = result.Conflicts });
        if (result.Conflicts.Count == 0) return ExitCodes.Success;
        OutputFormatter.WriteError(format, "Cratis-managed AI files were modified or a user-owned path conflicts; no files were changed.", "Review the reported paths. Use --force only to replace or remove content already recorded as Cratis-managed.", ExitCodes.ValidationErrorCode);
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
        return Task.FromResult(Write(AiCorpusSynchronizer.Synchronize(Directory.GetCurrentDirectory(), source, configuration, settings.Force), settings.ResolveOutputFormat()));
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
    "Bring this repository's Cratis-managed AI content up to the current corpus, reusing the selection already recorded in .cratis/ai.json. A locally edited managed file is reported as a conflict and left alone unless --force is given",
    Branch = typeof(AiBranch))]
[CliExample("ai", "update")]
[CliExample("ai", "update", "--source", "../AI")]
public sealed class AiUpdateCommand : AsyncCommand<AiSettings>
{
    protected override Task<int> ExecuteAsync(CommandContext context, AiSettings settings, CancellationToken cancellationToken)
    {
        var project = Directory.GetCurrentDirectory();
        var configuration = AiCorpusSynchronizer.Status(project).Configuration;
        return Task.FromResult(AiInstallCommand.Write(AiCorpusSynchronizer.Synchronize(project, AiInstallCommand.Source(settings), configuration, settings.Force), settings.ResolveOutputFormat()));
    }
}

/// <summary>Displays Cratis AI configuration and locally modified managed files.</summary>
/// <remarks>Read-only. Reports the installed revision, the revision available, and any managed file edited locally.</remarks>
[CliCommand(
    "status",
    "Show what is configured here, which corpus revision is installed, whether a newer one is available, and which managed files were edited locally. Changes nothing",
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
        });
        return Task.FromResult(status.ModifiedFiles.Count == 0 ? ExitCodes.Success : ExitCodes.ValidationError);
    }
}

/// <summary>Removes unchanged Cratis-managed AI content while preserving user files.</summary>
[CliCommand("uninstall", "Remove Cratis-owned AI content while preserving user-owned files", Branch = typeof(AiBranch))]
public sealed class AiUninstallCommand : AsyncCommand<AiUninstallSettings>
{
    protected override Task<int> ExecuteAsync(CommandContext context, AiUninstallSettings settings, CancellationToken cancellationToken) =>
        Task.FromResult(AiInstallCommand.Write(AiCorpusSynchronizer.Uninstall(Directory.GetCurrentDirectory(), settings.Force), settings.ResolveOutputFormat()));
}
