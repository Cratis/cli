// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;
using Cratis.Templating;
using Cratis.Templating.Packages;

namespace Cratis.Cli.Specs.for_TemplatePackageSource.when_reading_acquired_packages;

public class with_validated_store_paths : Specification
{
    string _root = null!;
    string _storeRoot = null!;
    TemplatePackageSource _source = null!;

    void Establish()
    {
        _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-source-specs", Guid.NewGuid().ToString("N"))).FullName;
        _storeRoot = Directory.CreateDirectory(Path.Combine(_root, "store")).FullName;
        _source = new TemplatePackageSource(_storeRoot, _root);
    }

    [Theory]
    [InlineData("..", "1.0.0")]
    [InlineData("Test.Pkg", "../../outside")]
    void should_treat_invalid_catalog_tokens_as_not_acquired(string id, string version)
    {
        var outside = Directory.CreateDirectory(Path.GetFullPath(Path.Combine(_storeRoot, id, version))).FullName;
        File.WriteAllText(Path.Combine(outside, ".cratis-complete"), string.Empty);
        _source.FindAcquired(id, version).ShouldBeNull();
        _source.AcquiredVersions(id).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("package")]
    [InlineData("version")]
    [InlineData("marker")]
    [InlineData("asset")]
    async Task should_agree_with_acquisition_about_linked_caches(string linkedPart)
    {
        var outside = Directory.CreateDirectory(Path.Combine(_root, "outside")).FullName;
        await File.WriteAllTextAsync(Path.Combine(outside, ".cratis-complete"), string.Empty, TestContext.Current.CancellationToken);
        var destination = Directory.CreateDirectory(Path.Combine(_storeRoot, "Test.Pkg", "1.0.0")).FullName;
        await File.WriteAllTextAsync(Path.Combine(destination, ".cratis-complete"), string.Empty, TestContext.Current.CancellationToken);
        CreateLink(linkedPart, destination, outside);
        var store = new TemplatePackageStore(_storeRoot);
        var error = await Catch.Exception(() => store.Acquire(new NuGetFeed("unused", outside), "Test.Pkg", "1.0.0"));
        error.ShouldBeOfExactType<TemplatePackageAcquisitionError>();
        _source.FindAcquired("Test.Pkg", "1.0.0").ShouldBeNull();
        _source.AcquiredVersions("Test.Pkg").ShouldBeEmpty();
    }

    [Fact]
    void should_recognize_valid_legacy_caches_and_ignore_invalid_version_names()
    {
        foreach (var version in new[] { "1.0.0+build.7", "not-a-version" })
        {
            var directory = Directory.CreateDirectory(Path.Combine(_storeRoot, "Test.Pkg", version)).FullName;
            File.WriteAllText(Path.Combine(directory, ".cratis-complete"), "legacy");
        }
        _source.AcquiredVersions("Test.Pkg").ShouldContainOnly("1.0.0+build.7");
        _source.FindAcquired("Test.Pkg", "1.0.0+build.7").ShouldEqual(Path.Combine(_storeRoot, "Test.Pkg", "1.0.0+build.7"));
    }

    static void CreateLink(string linkedPart, string destination, string outside)
    {
        var link = linkedPart switch
        {
            "package" => Path.GetDirectoryName(destination),
            "version" => destination,
            "marker" => Path.Combine(destination, ".cratis-complete"),
            _ => Path.Combine(destination, "asset.txt")
        };
        if (linkedPart.Equals("package", StringComparison.Ordinal) || linkedPart.Equals("version", StringComparison.Ordinal))
        {
            Directory.Delete(link, recursive: true);
            Directory.CreateSymbolicLink(link, outside);
        }
        else
        {
            File.Delete(link);
            File.CreateSymbolicLink(link, Path.Combine(outside, ".cratis-complete"));
        }
    }

    void Destroy() => Directory.Delete(_root, recursive: true);
}
