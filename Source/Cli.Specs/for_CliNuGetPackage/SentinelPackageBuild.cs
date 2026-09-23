// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;

namespace Cratis.Cli.for_CliNuGetPackage;

static class SentinelPackageBuild
{
    const string PropertyPrefix = "CliPackage.Property.";

    internal static ProcessStartInfo FromCurrentBuild(string outputDirectory, string version)
    {
        var metadata = typeof(SentinelPackageBuild).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToArray();
        var projectPath = metadata.Single(attribute => attribute.Key == "CliPackage.ProjectPath").Value!;
        var properties = metadata
            .Where(attribute => attribute.Key.StartsWith(PropertyPrefix, StringComparison.Ordinal))
            .Select(attribute => new KeyValuePair<string, string>(attribute.Key[PropertyPrefix.Length..], attribute.Value!));

        return CreateStartInfo(projectPath, properties, outputDirectory, version);
    }

    internal static ProcessStartInfo CreateStartInfo(string projectPath, IEnumerable<KeyValuePair<string, string>> properties, string outputDirectory, string version)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(projectPath),
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("pack");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--no-restore");
        startInfo.ArgumentList.Add("--output");
        startInfo.ArgumentList.Add(outputDirectory);
        startInfo.ArgumentList.Add($"-p:PackageVersion={version}");
        foreach (var property in properties.Where(property => !string.IsNullOrEmpty(property.Value)))
        {
            startInfo.ArgumentList.Add($"-p:{property.Key}={property.Value}");
        }

        return startInfo;
    }
}
