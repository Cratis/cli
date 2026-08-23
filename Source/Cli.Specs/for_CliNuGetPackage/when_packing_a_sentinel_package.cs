// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;

namespace Cratis.Cli.for_CliNuGetPackage;

public class when_packing_a_sentinel_package : Specification
{
    const string SentinelVersion = "0.0.0-package-metadata";
    const string PackageDescription = "Command-line tool for managing and exploring Chronicle event stores";

    string _outputDirectory;
    XDocument _nuspec;
    XDocument _toolSettings;
    JsonDocument _dependencies;
    string[] _packageEntries;

    void Establish() => _outputDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"cratis-cli-package-{Guid.NewGuid():N}")).FullName;

    async Task Because()
    {
        var repositoryRoot = FindRepositoryRoot();
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("pack");
        startInfo.ArgumentList.Add("Source/Cli/Cli.csproj");
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("--no-restore");
        startInfo.ArgumentList.Add("--output");
        startInfo.ArgumentList.Add(_outputDirectory);
        startInfo.ArgumentList.Add($"-p:PackageVersion={SentinelVersion}");

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new PackageMetadataVerificationFailed("Could not start dotnet pack for package metadata verification.");
        }

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = await standardOutput;
        var error = await standardError;
        if (process.ExitCode != 0)
        {
            throw new PackageMetadataVerificationFailed($"Sentinel package failed with exit code {process.ExitCode}.{Environment.NewLine}{output}{Environment.NewLine}{error}");
        }

        var packagePath = Path.Combine(_outputDirectory, $"Cratis.Cli.{SentinelVersion}.nupkg");
        await using var archive = await ZipFile.OpenReadAsync(packagePath);
        _packageEntries = [.. archive.Entries.Select(entry => entry.FullName)];
        _nuspec = await ReadXml(archive, "Cratis.Cli.nuspec");
        _toolSettings = await ReadXml(archive, "tools/net10.0/any/DotnetToolSettings.xml");
        _dependencies = await ReadJson(archive, "tools/net10.0/any/Cratis.Cli.deps.json");
    }

    [Fact]
    void should_include_complete_user_facing_metadata()
    {
        MetadataValue("id").ShouldEqual("Cratis.Cli");
        MetadataValue("version").ShouldEqual(SentinelVersion);
        MetadataValue("description").ShouldEqual(PackageDescription);

        var license = MetadataElement("license");
        license.Attribute("type")?.Value.ShouldEqual("expression");
        license.Value.ShouldEqual("MIT");

        MetadataValue("icon").ShouldEqual("logo.png");
        MetadataValue("readme").ShouldEqual("README.md");

        var repository = MetadataElement("repository");
        repository.Attribute("type")?.Value.ShouldEqual("git");
        string.IsNullOrWhiteSpace(repository.Attribute("commit")?.Value).ShouldBeFalse();

        _packageEntries.Contains("LICENSE", StringComparer.Ordinal).ShouldBeTrue();
        _packageEntries.Contains("README.md", StringComparer.Ordinal).ShouldBeTrue();
        _packageEntries.Contains("logo.png", StringComparer.Ordinal).ShouldBeTrue();

        var command = _toolSettings.Descendants().Single(element => element.Name.LocalName == "Command");
        command.Attribute("Name")?.Value.ShouldEqual("cratis");
        command.Attribute("EntryPoint")?.Value.ShouldEqual("Cratis.Cli.dll");
        command.Attribute("Runner")?.Value.ShouldEqual("dotnet");
    }

    [Fact]
    void should_freeze_the_current_atomic_adapter_package_closure()
    {
        RelevantDependencyLibraries().ShouldContainOnly(
        [
            "Cratis.Arc.Screenplay/22.0.0",
            "Cratis.CritterStack.Screenplay/0.17.0",
            "Cratis.Screenplay.Generation.Contracts/0.7.1",
            "Cratis.Screenplay.Generation.DotNet.Vogen/0.7.1",
            "Cratis.Screenplay.Generation.DotNet/0.7.1",
            "Cratis.Screenplay.Generation/0.7.1"
        ]);
    }

    [Fact]
    void should_bundle_the_cratis_vogen_adapter_without_target_vogen_assets()
    {
        _dependencies.RootElement.GetProperty("libraries").EnumerateObject()
            .Select(library => library.Name)
            .Any(library => library.StartsWith("Vogen/", StringComparison.Ordinal))
            .ShouldBeFalse();
        _packageEntries.Any(entry =>
            Path.GetFileName(entry).StartsWith("Vogen", StringComparison.OrdinalIgnoreCase) &&
            entry.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
        _packageEntries.Any(entry =>
            entry.Contains("/analyzers/", StringComparison.OrdinalIgnoreCase) &&
            entry.Contains("Vogen", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    }

    void Destroy()
    {
        _dependencies?.Dispose();
        if (Directory.Exists(_outputDirectory))
        {
            Directory.Delete(_outputDirectory, true);
        }
    }

    string MetadataValue(string name) => MetadataElement(name).Value;

    XElement MetadataElement(string name) =>
        _nuspec.Descendants().Single(element => element.Name.LocalName == name);

    string[] RelevantDependencyLibraries() =>
    [
        .. _dependencies.RootElement.GetProperty("libraries").EnumerateObject()
            .Select(library => library.Name)
            .Where(library =>
                library.StartsWith("Cratis.Arc.Screenplay/", StringComparison.Ordinal) ||
                library.StartsWith("Cratis.CritterStack.Screenplay/", StringComparison.Ordinal) ||
                library.StartsWith("Cratis.Screenplay.Generation", StringComparison.Ordinal) ||
                library.StartsWith("Vogen/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
    ];

    static async Task<XDocument> ReadXml(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path) ?? throw new PackageMetadataVerificationFailed($"The sentinel package is missing '{path}'.");
        await using var stream = await entry.OpenAsync();

        return await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
    }

    static async Task<JsonDocument> ReadJson(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path) ?? throw new PackageMetadataVerificationFailed($"The sentinel package is missing '{path}'.");
        await using var stream = await entry.OpenAsync();

        return await JsonDocument.ParseAsync(stream);
    }

    static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cli.slnx")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        throw new PackageMetadataVerificationFailed("Could not locate the repository root for package metadata verification.");
    }
}

sealed class PackageMetadataVerificationFailed(string message) : Exception(message);
