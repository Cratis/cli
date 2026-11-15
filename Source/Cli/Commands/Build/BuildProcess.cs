// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// What a process the host ran came to.
/// </summary>
/// <param name="ExitCode">Its exit code.</param>
/// <param name="Output">What it wrote to its standard output.</param>
/// <param name="Error">What it wrote to its standard error.</param>
public sealed record BuildProcessResult(int ExitCode, string Output, string Error);

/// <summary>
/// Runs the processes the host starts: agents, generators, verification commands and Git.
/// </summary>
public static class BuildProcess
{
    /// <summary>
    /// Runs a program with arguments and waits for it.
    /// </summary>
    /// <param name="program">The program.</param>
    /// <param name="arguments">Its arguments.</param>
    /// <param name="workingDirectory">Where it runs.</param>
    /// <param name="environment">Environment values to add.</param>
    /// <param name="input">What to write to its standard input, or <see langword="null"/>.</param>
    /// <param name="onLine">Receives each line of output as it is written, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Kills the process and its children.</param>
    /// <returns>What it came to.</returns>
    public static async Task<BuildProcessResult> Run(
        string program,
        IEnumerable<string> arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment = null,
        string? input = null,
        Action<string>? onLine = null,
        CancellationToken cancellationToken = default)
    {
        var start = new ProcessStartInfo(program)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        foreach (var (name, value) in environment ?? new Dictionary<string, string>()) start.Environment[name] = value;

        using var process = new Process { StartInfo = start };
        var output = new StringBuilder();
        var error = new StringBuilder();
        process.OutputDataReceived += (_, line) => Append(output, line.Data, onLine);
        process.ErrorDataReceived += (_, line) => Append(error, line.Data, onLine);

        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            return new(127, string.Empty, $"{program} could not be started: {exception.Message}");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        if (input is not null) await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw;
        }

        return new(process.ExitCode, output.ToString(), error.ToString());
    }

    /// <summary>
    /// Runs a command line through the platform's shell, the way a kit's verification commands and an exec hook are written.
    /// </summary>
    /// <param name="command">The command line.</param>
    /// <param name="workingDirectory">Where it runs.</param>
    /// <param name="environment">Environment values to add.</param>
    /// <param name="onLine">Receives each line of output, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Kills the command.</param>
    /// <returns>What it came to.</returns>
    public static Task<BuildProcessResult> Shell(
        string command,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment = null,
        Action<string>? onLine = null,
        CancellationToken cancellationToken = default) =>
        OperatingSystem.IsWindows()
            ? Run("cmd.exe", ["/d", "/s", "/c", command], workingDirectory, environment, null, onLine, cancellationToken)
            : Run("/bin/sh", ["-c", command], workingDirectory, environment, null, onLine, cancellationToken);

    static void Append(StringBuilder builder, string? line, Action<string>? onLine)
    {
        if (line is null) return;
        lock (builder)
        {
            builder.AppendLine(line);
        }

        onLine?.Invoke(line);
    }
}
