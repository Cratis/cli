// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;
using Cratis.Screenplay.Mcp;

namespace Cratis.Cli.Commands.Screenplay;

internal static class ScreenplayReleaseBaseline
{
    internal static int Execute(CommandContext context, BaselineScreenplaySettings settings, Func<string, ReleaseBaselineOutcome> run)
    {
        var format = settings.ResolveOutputFormat();
        if (context.Remaining.Parsed.Count > 0 || context.Remaining.Raw.Count > 0)
        {
            var option = context.Remaining.Raw.Count > 0 ? context.Remaining.Raw[0] : context.Remaining.Parsed.First().Key;
            OutputFormatter.WriteError(format, $"Unknown option '{option}'.", "Run 'cratis screenplay baseline --help' for usage", "CLI-BASELINE-USAGE");
            return -1;
        }

        ReleaseBaselineOutcome outcome;
        try
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var root = ScreenplayMcpRoot.Resolve(settings.Root, null, null, currentDirectory, Environment.GetEnvironmentVariable) ?? currentDirectory;
            outcome = run(root);
        }
        catch (Exception exception) when (exception is AiMcpConfigurationInvalid or ScreenplayWorkspaceStateConflict or ScreenplayWorkspaceMetadataConflict or IOException or UnauthorizedAccessException)
        {
            outcome = new(2, [], exception.GetType().Name, exception.Message);
        }

        OutputFormatter.WriteObject(format, outcome, result =>
        {
            foreach (var violation in result.Violations)
            {
                Console.WriteLine($"{violation.Kind}: {violation.Event} revision {violation.Revision}{(violation.Path is null ? string.Empty : $" at {violation.Path}")}");
            }
            if (result.AdmittedEntryCount is { } count)
            {
                Console.WriteLine($"Admitted {count} event revision(s) in release baseline {result.BaselineRevision}.");
            }
        });
        if (outcome.Reason is not null && format is not (OutputFormats.Json or OutputFormats.JsonCompact or OutputFormats.JsonQuiet))
        {
            Console.Error.WriteLine(outcome.Reason.StartsWith($"{outcome.FailureKind}:", StringComparison.Ordinal)
                ? outcome.Reason
                : $"{outcome.FailureKind}: {outcome.Reason}");
        }

        return outcome.ExitCode;
    }
}
