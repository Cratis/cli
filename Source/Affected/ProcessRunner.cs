// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Affected;

/// <summary>
/// Runs child processes with an argument list, never through a shell.
/// </summary>
public static class ProcessRunner
{
    /// <summary>
    /// Runs a process and captures its output.
    /// </summary>
    /// <param name="fileName">The executable.</param>
    /// <param name="arguments">The arguments, passed as separate values.</param>
    /// <param name="workingDirectory">The working directory.</param>
    /// <returns>The exit code and captured output.</returns>
    public static ProcessResult Run(string fileName, IEnumerable<string> arguments, string workingDirectory)
    {
        using var process = Start(fileName, arguments, workingDirectory, redirect: true);
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, output, error.GetAwaiter().GetResult());
    }

    /// <summary>
    /// Runs a process with its output passed straight through to this process.
    /// </summary>
    /// <param name="fileName">The executable.</param>
    /// <param name="arguments">The arguments, passed as separate values.</param>
    /// <param name="workingDirectory">The working directory.</param>
    /// <param name="cancellationToken">Cancels the wait and kills the process.</param>
    /// <returns>The exit code.</returns>
    public static async Task<int> RunInheritedAsync(string fileName, IEnumerable<string> arguments, string workingDirectory, CancellationToken cancellationToken = default)
    {
        using var process = Start(fileName, arguments, workingDirectory, redirect: false);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        return process.ExitCode;
    }

    static Process Start(string fileName, IEnumerable<string> arguments, string workingDirectory, bool redirect)
    {
        var info = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = redirect,
            RedirectStandardError = redirect,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        return Process.Start(info) ?? throw new AffectedException($"Could not start '{fileName}'.");
    }
}
