// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Cli.for_DirectMcpRegistration.given;

public static class unix_acl_tools
{
    internal static string Run(string command, params string[] arguments)
    {
        var start = new ProcessStartInfo(command) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var child = Process.Start(start)!;
        var output = child.StandardOutput.ReadToEndAsync();
        var errors = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(10000))
        {
            child.Kill(entireProcessTree: true);
            child.WaitForExit();
            throw new IOException("ACL fixture command timed out.");
        }
        var text = output.GetAwaiter().GetResult();
        var error = errors.GetAwaiter().GetResult();
        if (child.ExitCode != 0) throw new IOException($"ACL fixture command failed: {error}");
        return text;
    }

    internal static string Entries(string path) => OperatingSystem.IsMacOS()
        ? string.Join('\n', Run("/bin/ls", "-le", path).Split('\n').Skip(1))
        : Run("getfacl", "-cp", path);

    static bool Available(string name) => (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator).Any(directory => File.Exists(Path.Combine(directory, name)));

    public sealed class FactAttribute : Xunit.FactAttribute
    {
        public FactAttribute()
        {
            if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) Skip = "Requires Linux or macOS ACL support.";
            else if (OperatingSystem.IsLinux() && (!Available("setfacl") || !Available("getfacl"))) Skip = "Requires setfacl and getfacl.";
            else if (OperatingSystem.IsMacOS() && !File.Exists("/bin/chmod")) Skip = "Requires chmod +a.";
        }
    }
}
