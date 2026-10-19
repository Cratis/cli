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
    /// <returns>The command to run the matching extractor image.</returns>
    public static string Build(PrologueWizardInput input, string configurationPath)
    {
        var arguments = new List<string> { "docker run --rm" };
        if (input.Api is not null)
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

        var version = typeof(PrologueConfiguration).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0];
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

    static string HostCaptureDirectory(PrologueWizardInput input, string configurationPath) =>
        Path.GetFullPath(input.Output.JsonDirectory, Path.GetDirectoryName(configurationPath)!);

    static string Quote(string value) =>
        $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("$", "\\$", StringComparison.Ordinal).Replace("`", "\\`", StringComparison.Ordinal)}\"";
}
