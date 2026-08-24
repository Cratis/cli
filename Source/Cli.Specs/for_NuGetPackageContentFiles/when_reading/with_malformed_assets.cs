// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_NuGetPackageContentFiles.when_reading;

/// <summary>
/// Characterizes fail-closed handling of syntactically valid assets with unexpected shapes.
/// </summary>
public class with_malformed_assets : Specification
{
    string _fixtureRoot;
    IReadOnlySet<string> _nonObjectRoot;
    IReadOnlySet<string> _malformedContainers;
    IReadOnlySet<string> _malformedPackageMetadata;

    void Establish()
    {
        _fixtureRoot = Path.Combine(Path.GetTempPath(), $"screenplay-malformed-assets-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_fixtureRoot);
    }

    void Because()
    {
        _nonObjectRoot = Read("[]", "non-object-root.json");
        _malformedContainers = Read(
            "{\"packageFolders\":[],\"libraries\":{},\"targets\":{}}",
            "malformed-containers.json");
        _malformedPackageMetadata = Read(
            "{\"packageFolders\":{\"/packages\":{}},\"libraries\":{\"Package/1.0.0\":{\"path\":[]}},\"targets\":{\"net10.0\":{\"Package/1.0.0\":{\"type\":\"package\",\"contentFiles\":{\"contentFiles/cs/any/Content.cs\":{\"buildAction\":[]}}}}}}",
            "malformed-package.json");
    }

    [Fact] void should_ignore_a_non_object_root() => _nonObjectRoot.ShouldBeEmpty();
    [Fact] void should_ignore_malformed_root_containers() => _malformedContainers.ShouldBeEmpty();
    [Fact] void should_ignore_malformed_package_metadata() => _malformedPackageMetadata.ShouldBeEmpty();

    void Destroy()
    {
        if (!string.IsNullOrEmpty(_fixtureRoot) && Directory.Exists(_fixtureRoot))
        {
            Directory.Delete(_fixtureRoot, recursive: true);
        }
    }

    IReadOnlySet<string> Read(string content, string fileName)
    {
        var path = Path.Combine(_fixtureRoot, fileName);
        File.WriteAllText(path, content);
        return NuGetPackageContentFiles.From(path);
    }
}
