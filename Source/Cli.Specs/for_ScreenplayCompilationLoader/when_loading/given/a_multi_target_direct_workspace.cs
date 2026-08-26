// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.for_ScreenplayCompilationLoader.when_loading.given;

public class a_multi_target_direct_workspace : Specification
{
    protected string Root;
    protected string TargetPath;

    async Task Establish()
    {
        Root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"screenplay-direct-multitarget-{Guid.NewGuid():N}")).FullName;
        Directory.CreateDirectory(Path.Combine(Root, ".git"));
        WriteProject("Dependencies/Dependency.csproj");
        WriteProject("Host/Application.csproj", "../Dependencies/Dependency.csproj");
        TargetPath = Path.Combine(Root, "Host", "Application.csproj");
        await Restore(TargetPath);
    }

    void Destroy()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, true);
        }
    }

    void WriteProject(string relativePath, params string[] projectReferences)
    {
        var path = Path.Combine(Root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var references = string.Join(Environment.NewLine, projectReferences.Select(reference => $"    <ProjectReference Include=\"{reference}\" />"));
        var itemGroup = projectReferences.Length == 0 ? string.Empty : $"  <ItemGroup>{Environment.NewLine}{references}{Environment.NewLine}  </ItemGroup>{Environment.NewLine}";
        File.WriteAllText(
            path,
            $"<Project Sdk=\"Microsoft.NET.Sdk\">{Environment.NewLine}  <PropertyGroup><TargetFrameworks>net9.0;net10.0</TargetFrameworks></PropertyGroup>{Environment.NewLine}{itemGroup}</Project>{Environment.NewLine}");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(path)!, "Source.cs"), $"namespace {Path.GetFileNameWithoutExtension(path)}; public sealed class Marker;{Environment.NewLine}");
    }

    static async Task Restore(string projectPath)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(projectPath),
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("restore");
        startInfo.ArgumentList.Add(projectPath);
        using var process = Process.Start(startInfo) ?? throw new DirectProjectRestoreFailed("Could not start dotnet restore.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new DirectProjectRestoreFailed($"dotnet restore failed.{Environment.NewLine}{await output}{Environment.NewLine}{await error}");
        }
    }
}
