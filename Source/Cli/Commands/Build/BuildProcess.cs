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
    public static Task<BuildProcessResult> Run(
        string program,
        IEnumerable<string> arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment = null,
        string? input = null,
        Action<string>? onLine = null,
        CancellationToken cancellationToken = default) => Execute(BuildChildEnvironment.Start(program, arguments, workingDirectory, environment), input, onLine, cancellationToken);

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

    internal static ProcessStartInfo DockerHost(IReadOnlyList<string> arguments, string directory, IReadOnlyDictionary<string, string> environment, string token)
    {
        var start = BuildChildEnvironment.Start("docker", arguments, directory, environment);

        // Existing builder images bootstrap from a name-forwarded variable. Only their trusted launcher gets it.
        start.Environment[StudioConnection.BuilderTokenVariable] = token;
        return start;
    }

    internal static async Task<BuildProcessResult> Execute(ProcessStartInfo start, string? input, Action<string>? onLine, CancellationToken cancellationToken)
    {
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
            return new(127, string.Empty, $"{start.FileName} could not be started: {exception.Message}");
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
