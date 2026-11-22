// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Prologue.Configuration;

namespace Cratis.Cli.Commands.Prologue;

/// <summary>
/// Builds the commands for running the extractor with the wizard's configuration and interpreting its captures.
/// </summary>
public static class PrologueExtractorCommand
{
    /// <summary>
    /// The JSON capture directory inside the extractor container.
    /// </summary>
    public const string CaptureDirectory = "/captures";

    /// <summary>
    /// Builds a Docker command with the mounts and ports the selected sources and output need.
    /// </summary>
    /// <param name="input">The values entered in the wizard, including the host capture directory.</param>
    /// <param name="configurationPath">The absolute path of the configuration file written by the wizard.</param>
    /// <param name="user">
    /// The <c language="csharp">uid:gid</c> to run the container as so it can write to the host capture directory, as the image runs as a non-root user;
    /// <see langword="null"/> to leave the image user unchanged.
    /// </param>
    /// <returns>The command to run the matching extractor image.</returns>
    public static string Build(PrologueWizardInput input, string configurationPath, string? user = null)
    {
        var arguments = new List<string> { "docker run --rm" };
        if (!string.IsNullOrWhiteSpace(user) && input.Output.Kind == OutputKind.Json)
        {
            arguments.Add($"--user {user}");
        }

        if (input.CapturesApi)
        {
            arguments.Add("-p 8080:8080");
        }

        if (input.OpenTelemetry is not null)
        {
            arguments.Add("-p 4317:4317 -p 4318:4318");
        }

        arguments.Add($"-v {Quote($"{configurationPath}:/config/cratis-prologue.json:ro")}");
        if (input.Output.Kind == OutputKind.Json)
        {
            arguments.Add($"-v {Quote($"{HostCaptureDirectory(input, configurationPath)}:{CaptureDirectory}")}");
            arguments.Add($"-e Prologue__Output__Json__Directory={CaptureDirectory}");
        }

        var version = typeof(PrologueExtractorCommand).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(metadata => metadata.Key == "Cratis.Prologue.Configuration.PackageVersion")?.Value;
        arguments.Add(string.IsNullOrWhiteSpace(version) ? "cratis/prologue-extractor" : $"cratis/prologue-extractor:{version}");
        return string.Join(' ', arguments);
    }

    /// <summary>
    /// Builds the interpretation command pointing at the host's JSON capture directory.
    /// </summary>
    /// <param name="input">The values entered in the wizard.</param>
    /// <param name="configurationPath">The absolute path of the configuration file written by the wizard.</param>
    /// <returns>The interpretation command, or the existing Receiver output hint.</returns>
    public static string Interpret(PrologueWizardInput input, string configurationPath) =>
        input.Output.Kind == OutputKind.Json
            ? $"cratis prologue interpret {Quote(HostCaptureDirectory(input, configurationPath))}"
            : "cratis prologue interpret";

    /// <summary>
    /// Creates the host capture directory when missing, so it belongs to the current user rather than to Docker (root).
    /// Nothing is created for Receiver output, and the permissions of an existing directory are never changed.
    /// </summary>
    /// <param name="input">The values entered in the wizard.</param>
    /// <param name="configurationPath">The absolute path of the configuration file written by the wizard.</param>
    /// <returns>The host capture directory when JSON output is configured; otherwise <see langword="null"/>.</returns>
    public static string? PrepareCaptureDirectory(PrologueWizardInput input, string configurationPath)
    {
        if (input.Output.Kind != OutputKind.Json)
        {
            return null;
        }

        var directory = HostCaptureDirectory(input, configurationPath);
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>
    /// Gets the <c language="csharp">uid:gid</c> of the current user on Linux and macOS, which the extractor container runs as to write to
    /// the capture directory. On Windows with Docker Desktop, bind mounts are writable by the container user and host IDs
    /// do not exist, so there is none.
    /// </summary>
    /// <returns>The <c language="csharp">uid:gid</c>, or <see langword="null"/> when not applicable or not available.</returns>
    public static string? HostUser()
    {
        if (OperatingSystem.IsWindows())
        {
            return null;
        }

        var uid = RunId("-u");
        var gid = RunId("-g");
        return uid is null || gid is null ? null : $"{uid}:{gid}";
    }

    static string? RunId(string argument)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo("id", argument) { RedirectStandardOutput = true, UseShellExecute = false };
            using var process = System.Diagnostics.Process.Start(startInfo);
            var output = process?.StandardOutput.ReadToEnd().Trim();
            process?.WaitForExit();
            return process?.ExitCode == 0 && output is { Length: > 0 } && output.All(char.IsAsciiDigit) ? output : null;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return null;
        }
    }

    static string HostCaptureDirectory(PrologueWizardInput input, string configurationPath) =>
        Path.GetFullPath(input.Output.JsonDirectory, Path.GetDirectoryName(configurationPath)!);

    static string Quote(string value) =>
        $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("$", "\\$", StringComparison.Ordinal).Replace("`", "\\`", StringComparison.Ordinal)}\"";
}
