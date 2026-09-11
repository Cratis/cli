// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>Installs selected Cratis AI guidance into the current repository.</summary>
[CliCommand("install", "Install selected Cratis AI rules, skills, and harness integration", Branch = typeof(AiBranch))]
public sealed class AiInstallCommand : AsyncCommand<AiInstallSettings>
{
    public static int Write(SyncResult result, string format)
    {
        OutputFormatter.WriteObject(format, new { actions = result.Actions, conflicts = result.Conflicts });
        if (result.Conflicts.Count == 0) return ExitCodes.Success;
        OutputFormatter.WriteError(format, "Cratis-managed AI files were modified locally; no files were changed.", "Review the reported files, or re-run with --force to replace or remove them.", ExitCodes.ValidationErrorCode);
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
            Select(settings.Languages, "languages", available.Languages));
        return Task.FromResult(Write(AiCorpusSynchronizer.Synchronize(Directory.GetCurrentDirectory(), source, configuration, settings.Force), settings.ResolveOutputFormat()));
    }

    static string[] Select(string? value, string label, IReadOnlyList<string> defaults)
    {
        if (!string.IsNullOrWhiteSpace(value)) return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (Console.IsInputRedirected) throw new InvalidOperationException($"--{label} is required when input is redirected.");
        var prompt = new MultiSelectionPrompt<string>()
            .Title($"Select {label}")
            .AddChoices(defaults);
        return [.. AnsiConsole.Prompt(prompt)];
    }
}

/// <summary>Synchronizes the configured Cratis AI corpus.</summary>
[CliCommand("update", "Synchronize configured Cratis-owned AI content without overwriting local changes", Branch = typeof(AiBranch))]
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
[CliCommand("status", "Show Cratis AI configuration, installed revision, and local conflicts", Branch = typeof(AiBranch))]
public sealed class AiStatusCommand : AsyncCommand<GlobalSettings>
{
    protected override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var status = AiCorpusSynchronizer.Status(Directory.GetCurrentDirectory());
        OutputFormatter.WriteObject(settings.ResolveOutputFormat(), new { harnesses = status.Configuration.Harnesses, profiles = status.Configuration.Profiles, languages = status.Configuration.Languages, sourceRevision = status.SourceRevision, modifiedFiles = status.ModifiedFiles });
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
