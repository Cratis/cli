// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>Uses public application locations and Windows' public package discovery command.</summary>
internal static class DesktopMcpApplications
{
    internal static string? Find(DesktopMcpPlatform platform, string name)
    {
        if (platform.Os == "osx")
            return new[] { $"/Applications/{name}.app", Path.Combine(platform.Home, "Applications", $"{name}.app") }.FirstOrDefault(Directory.Exists);
        if (platform.Os != "win") return null;
        var executable = $"{name}.exe";
        var found = new[]
        {
            Path.Combine(platform.LocalApplications, name, executable),
            Path.Combine(platform.LocalApplications, "Programs", name, executable),
            Path.Combine(platform.LocalApplications, "Microsoft", "WindowsApps", executable),
            Path.Combine(platform.ProgramFiles, name, executable)
        }.FirstOrDefault(File.Exists);
        if (found is not null || !OperatingSystem.IsWindows()) return found;
        var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-NonInteractive");
        start.ArgumentList.Add("-Command");
        start.ArgumentList.Add(name == "ChatGPT" ? "Get-AppxPackage *ChatGPT* | Select-Object -ExpandProperty InstallLocation" : "Get-AppxPackage *Claude* | Select-Object -ExpandProperty InstallLocation");
        try
        {
            using var process = Process.Start(start);
            if (process is null) return null;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(5000))
            {
                process.Kill(entireProcessTree: true);
                return null;
            }
            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output.GetAwaiter().GetResult()) ? name : null;
        }
        catch (Exception)
        {
            // Detection is advisory; explicit client selection can still stage a package.
            return null;
        }
    }

    internal static ProcessStartInfo OpenCommand(DesktopMcpPlatform platform, string path)
    {
        var start = platform.Os == "osx"
            ? new ProcessStartInfo("/usr/bin/open") { UseShellExecute = false, RedirectStandardError = true }
            : new ProcessStartInfo(path) { UseShellExecute = true };
        if (platform.Os == "osx")
        {
            start.ArgumentList.Add("-a");
            start.ArgumentList.Add("Claude");
            start.ArgumentList.Add(path);
        }
        return start;
    }

    internal static void Open(DesktopMcpPlatform platform, string path)
    {
        using var process = Process.Start(OpenCommand(platform, path));

        // ShellExecute may successfully hand the file to an existing Windows process and return null.
        if (platform.Os != "osx") return;
        if (process is null) throw new InvalidOperationException("Claude could not be opened. Select the bundle in Settings > Extensions > Advanced settings.");
        var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(10000))
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException("Claude's open-file request timed out. Open the bundle manually in Settings > Extensions.");
        }
        if (process.ExitCode != 0) throw new InvalidOperationException($"Claude could not open the bundle: {error.GetAwaiter().GetResult().Trim()}. Select it in Settings > Extensions > Advanced settings.");
    }
}
