// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Integration.Chronicle;

/// <summary>
/// Exception that gets thrown when something written through the CLI never became visible to a read that
/// polled for it.
/// </summary>
/// <param name="description">Describes what was being waited for.</param>
/// <param name="args">The arguments of the CLI command that was polled.</param>
/// <param name="attempts">The number of times the command was run.</param>
/// <param name="elapsed">The time spent waiting.</param>
/// <param name="lastResult">The result of the last attempt.</param>
public class ReadBackTimedOut(string description, IEnumerable<string> args, int attempts, TimeSpan elapsed, CliCommandResult lastResult)
    : Exception(BuildMessage(description, args, attempts, elapsed, lastResult))
{
    const int MaximumReportedOutputLength = 4000;

    static string BuildMessage(string description, IEnumerable<string> args, int attempts, TimeSpan elapsed, CliCommandResult lastResult) =>
        $"{description} never showed up in the output of `cratis {string.Join(' ', args)}`. " +
        $"Polled {attempts} time(s) over {elapsed.TotalSeconds:0.0}s.{Environment.NewLine}" +
        $"Last exit code: {lastResult.ExitCode}{Environment.NewLine}" +
        $"Last standard output: {Truncate(lastResult.StandardOutput)}{Environment.NewLine}" +
        $"Last standard error: {Truncate(lastResult.StandardError)}";

    static string Truncate(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "(empty)";
        }

        return value.Length <= MaximumReportedOutputLength
            ? value
            : $"{value[..MaximumReportedOutputLength]}… (truncated, {value.Length} characters in total)";
    }
}
