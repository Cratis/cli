// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Runs the kit's verification commands, and any given besides, in order in the project. The host runs them, not the agent:
/// a slice is reported built only when every one exits with 0.
/// </summary>
/// <param name="options">How the host builds.</param>
public sealed class BuildVerification(BuildHostOptions options)
{
    /// <summary>
    /// Runs every verification command.
    /// </summary>
    /// <param name="order">The order being built.</param>
    /// <param name="values">The placeholder values for the order.</param>
    /// <param name="cancellationToken">Stops the commands.</param>
    /// <returns>Why verification failed, or <see langword="null"/> when every command passed.</returns>
    public async Task<string?> Run(BuildOrder order, IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken)
    {
        foreach (var template in (options.Kit?.Verify ?? []).Concat(options.Verify))
        {
            var command = BuildKitPlaceholders.FillCommand(template, values);
            options.Log($"Verifying: {command}");
            var result = await BuildProcess.Shell(command, options.Project, null, line => options.Log(options.Redaction.Redact(line)), cancellationToken);
            if (result.ExitCode != 0)
            {
                return $"Verification failed: `{command}` exited with {result.ExitCode}.{Tail(result)}";
            }
        }

        return null;
    }

    static string Tail(BuildProcessResult result)
    {
        var lines = $"{result.Output}\n{result.Error}".Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var tail = string.Join('\n', lines.TakeLast(20));
        return tail.Length == 0 ? string.Empty : $"\n{(tail.Length > 1500 ? tail[^1500..] : tail)}";
    }
}
