// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Cli.Commands.Ai.Testing;

internal sealed record AiTestCheckResult(string Type, AiTestPhase Phase, string Status, string Evidence, bool Optional = false);
internal sealed record AiTestFileSelection(AiTestCheck Check, string[] Paths);

internal sealed class AiTestChecks(IAiTestProcess process, AiTestRedactor redactor)
{
    internal async Task<AiTestCheckResult> Evaluate(AiTestCheck check, string workspace, TimeSpan timeout, CancellationToken cancellationToken)
    {
        try
        {
            check.Validate();
            if (check.Type == "command") return await Command(check, workspace, timeout, cancellationToken);
            var paths = AiTestPaths.Select(check, workspace);
            if (paths.Length == 0 && check.Type == "file_matches") return Result(check, "failed", "zero files matched");
            var passed = EvaluateFiles(new(check, paths));
            return Result(check, passed ? "passed" : "failed", $"{check.Path ?? check.Glob}: {paths.Length} matching file(s).");
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException or RegexMatchTimeoutException)
        {
            return Result(check, "error", exception.Message);
        }
    }

    static bool EvaluateFiles(AiTestFileSelection selection)
    {
        if (selection.Paths.Length == 0) return selection.Check.Type == "file_absent";
        return selection.Check.Type switch
        {
            "file_exists" => true,
            "file_absent" => false,
            "file_matches" => AllMatch(selection),
            "file_not_matches" => NoneMatch(selection),
            _ => false
        };
    }

    static bool AllMatch(AiTestFileSelection selection) => selection.Paths.All(path => Matches(path, selection.Check));

    static bool NoneMatch(AiTestFileSelection selection) => !selection.Paths.Any(path => Matches(path, selection.Check));

    static bool Matches(string path, AiTestCheck check) => Regex.IsMatch(File.ReadAllText(path), check.Pattern!, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    async Task<AiTestCheckResult> Command(AiTestCheck check, string workspace, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var result = await process.Run(new(check.Run[0], check.Run[1..], workspace), timeout, cancellationToken);
        var status = "error";
        if (result.Status == "completed") status = result.ExitCode == check.ExitCode ? "passed" : "failed";
        if (check.Optional && result.Status == "skipped") status = "skipped";
        return Result(check, status, $"exit_code={result.ExitCode}; {result.Reason} {result.Output}");
    }

    AiTestCheckResult Result(AiTestCheck check, string status, string evidence) => new(check.Type, check.Phase, status, redactor.Redact(evidence), check.Optional);
}
