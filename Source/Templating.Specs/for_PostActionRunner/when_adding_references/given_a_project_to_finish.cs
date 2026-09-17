// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Text;
using Cratis.Templating.Configuration;
using Cratis.Templating.Packages;
using Cratis.Templating.PostActions;

namespace Cratis.Templating.Specs.for_PostActionRunner.when_adding_references;

public abstract class given_a_project_to_finish : Specification
{
    protected string OutputRoot = null!;
    protected string FeedRoot = null!;
    protected string? PreviousCurrentDirectory;

    void Establish()
    {
        OutputRoot = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-postactions", Guid.NewGuid().ToString("N"))).FullName;

        // A local NuGet feed with one stub package, exposed through a repository-level NuGet.Config
        // whose directory becomes the working directory, so resolution runs fully offline.
        FeedRoot = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-feed", Guid.NewGuid().ToString("N"))).FullName;
        using (var stream = File.Create(Path.Combine(FeedRoot, "Some.Package.3.2.1.nupkg")))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("lib/net10.0/_");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("stub");
        }
        File.WriteAllText(Path.Combine(FeedRoot, "nuget.config"),
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<configuration>\n  <packageSources>\n    <add key=\"spec-feed\" value=\"REPLACED\" />\n  </packageSources>\n</configuration>".Replace("REPLACED", FeedRoot));

        PreviousCurrentDirectory = Environment.CurrentDirectory;
        Environment.CurrentDirectory = FeedRoot;
    }

    protected string WriteProject(string content)
    {
        var path = Path.Combine(OutputRoot, "App.csproj");
        File.WriteAllText(path, content);
        return path;
    }

    protected InstantiationResult ResultFor(params string[] outputs) => new(
        "App",
        OutputRoot,
        [],
        [.. outputs.Select(output => Path.Combine(OutputRoot, output))],
        new Dictionary<string, string>(),
        []);

    protected static PostActionConfig PackageReferenceAction(string packageId) => new()
    {
        ActionId = "B17581D1-C5C9-4489-8F0A-004BE667B814",
        Args = new Dictionary<string, string>
        {
            ["referenceType"] = "package",
            ["reference"] = packageId
        },
        ManualInstructions = [new ManualInstructionConfig { Text = "Add it manually." }]
    };

    void Destroy()
    {
        if (PreviousCurrentDirectory is not null)
        {
            Environment.CurrentDirectory = PreviousCurrentDirectory;
        }
    }
}
