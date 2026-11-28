// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;

namespace Cratis.Cli.Commands.Build;

/// <summary>Prevents workloads from inheriting credentials owned by the builder host.</summary>
internal static class BuildChildEnvironment
{
    internal static ProcessStartInfo Start(string program, IEnumerable<string> arguments, string workingDirectory, IReadOnlyDictionary<string, string>? environment)
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
        Apply(start, environment);
        return start;
    }

    internal static void Apply(ProcessStartInfo start, IReadOnlyDictionary<string, string>? overrides = null)
    {
        foreach (var (key, value) in overrides ?? new Dictionary<string, string>()) start.Environment[key] = value;
        foreach (var key in start.Environment.Keys.Where(IsPrivate).ToArray()) start.Environment.Remove(key);
    }

    static bool IsPrivate(string key) => key.Equals(StudioConnection.BuilderTokenVariable, StringComparison.OrdinalIgnoreCase) ||
        key.Equals(BuildConnectSettings.McpSecretVariable, StringComparison.OrdinalIgnoreCase) || key.Equals(BuilderBootstrap.HandoffVariable, StringComparison.OrdinalIgnoreCase);
}
