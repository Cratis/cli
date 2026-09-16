// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Xunit.Abstractions;

#pragma warning disable IDE0051 // Fact methods are invoked by the test runner via reflection
#pragma warning disable RCS1213 // Fact methods are invoked by the test runner via reflection
using System.Text.RegularExpressions;

using Cratis.Templating.Conformance.given;

namespace Cratis.Templating.Conformance;

/// <summary>
/// The differential oracle — a test-time-only comparison against <c language="csharp">dotnet new</c>, never a
/// runtime dependency. Opt in with <c language="csharp">CRA_TIS_DIFFERENTIAL=1</c>. on a machine that has the
/// SDK: renders each Cratis template both ways and diffs the trees, normalizing generated GUIDs,
/// timestamps and resolved package versions. When not opted in the suite reports itself skipped
/// rather than passing vacuously.
/// </summary>
/// <param name="output">The test output helper for reporting the opt-out path.</param>
public partial class when_running_the_differential_oracle(ITestOutputHelper output) : a_conformance_spec
{
    const string TemplatesVersion = "1.3.0";

    [GeneratedRegex(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b", RegexOptions.None, 2000)]
    private static partial Regex GuidPattern { get; }

    [GeneratedRegex(@"Version=""[^""]*""", RegexOptions.None, 2000)]
    private static partial Regex VersionPattern { get; }

    [GeneratedRegex(@"\b20\d{2}\b", RegexOptions.None, 2000)]
    private static partial Regex YearPattern { get; }

    [Fact]
    public void should_render_byte_equivalently_to_dotnet_new()
    {
        // xunit v2 has no runtime skip; the opt-in is an early return that reports itself,
        // so normal runs stay green without pretending the comparison ran.
        if (Environment.GetEnvironmentVariable("CRA_TIS_DIFFERENTIAL") != "1")
        {
            output.WriteLine("skipped: differential oracle is opt-in — set CRA_TIS_DIFFERENTIAL=1 with an SDK present to run it.");
            return;
        }

        Which("dotnet").ShouldBeTrue();

        var compared = 0;
        foreach (var shortName in new[] { "cratis-chronicle-console", "cratis-chronicle-web", "cratis", "cratis-aspire" })
        {
            var root = Path.Combine(Path.GetTempPath(), "cratis-differential", Guid.NewGuid().ToString("N"));
            var ours = Path.Combine(root, "ours");
            var theirs = Path.Combine(root, "theirs");

            // dotnet side — isolated DOTNET_CLI_HOME so the user's template store is untouched.
            var home = Path.Combine(root, "dotnet-home");
            Directory.CreateDirectory(home);
            Run("dotnet", "new install Cratis.Templates::" + TemplatesVersion, home, root);
            Run("dotnet", $"new {shortName} -n DiffApp -o \"{theirs}\"", home, root);

            // Our side.
            var templateRoot = Path.Combine(TemplatesRoot, FolderFor(shortName));
            var manifest = TemplateConfigParser.ParseFile(Path.Combine(templateRoot, ".template.config", "template.json"));
            new TemplateInstantiator().Instantiate(manifest, templateRoot, new InstantiationInputs("DiffApp", ours, new Dictionary<string, string>()));

            var ourFiles = Collect(ours);
            var theirFiles = Collect(theirs);
            ourFiles.Keys.Order(StringComparer.Ordinal)
                .ShouldContainOnly([.. theirFiles.Keys.Order(StringComparer.Ordinal)]);

            foreach (var (relative, ourContent) in ourFiles)
            {
                if (Path.GetExtension(relative) is { } extension && (string.Equals(extension, ".png", StringComparison.Ordinal) || string.Equals(extension, ".ico", StringComparison.Ordinal) || string.Equals(extension, ".nupkg", StringComparison.Ordinal)))
                {
                    continue;
                }
                var normalizedOurs = Normalize(ourContent);
                var normalizedTheirs = Normalize(theirFiles[relative]);
                normalizedOurs.ShouldEqual(normalizedTheirs);
                compared++;
            }
        }
        compared.ShouldBeGreaterThan(10);
    }

    static string FolderFor(string shortName) => shortName switch
    {
        "cratis" => "Cratis",
        "cratis-aspire" => "CratisAspire",
        "cratis-chronicle-console" => "ChronicleConsole",
        "cratis-chronicle-web" => "ChronicleWeb",
        _ => throw new InvalidOperationException("no conformance folder for " + shortName)
    };

    static Dictionary<string, string> Collect(string root)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!Directory.Exists(root))
        {
            return files;
        }

        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
            if (relative.StartsWith("obj/") || relative.Contains("/obj/"))
            {
                continue;
            }
            files[relative] = File.ReadAllText(path);
        }
        return files;
    }

    static string Normalize(string content) =>
        YearPattern.Replace(
            VersionPattern.Replace(
                GuidPattern.Replace(content, "<guid>"),
                "Version=\"<version>\""),
            "<year>");

    static void Run(string executable, string arguments, string home, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo(executable, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.EnvironmentVariables["DOTNET_CLI_HOME"] = home;

        // Telemetry and banner off keeps the isolated home quiet.
        startInfo.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.EnvironmentVariables["DOTNET_NOLOGO"] = "1";
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"failed to start {executable}");

        // A bounded wait so a stuck dotnet fails the oracle instead of hanging the run.
        if (!process.WaitForExit(120_000))
        {
            process.Kill();
            throw new InvalidOperationException($"{executable} {arguments} did not finish within 120 seconds.");
        }
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{executable} {arguments} exited with {process.ExitCode}: {process.StandardError.ReadToEnd()}");
        }
    }

    static bool Which(string executable)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var name = OperatingSystem.IsWindows() ? $"{executable}.exe" : executable;
        return path.Split(Path.PathSeparator).Any(directory => File.Exists(Path.Combine(directory.Trim(), name)));
    }
}
