// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_NuGetClient.when_extracting_a_package;

public class with_existing_destinations : Specification
{
    string _root = null!;

    void Establish() => _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-direct-specs", Guid.NewGuid().ToString("N"))).FullName;

    [Fact]
    void should_preserve_successful_overwrite_behavior()
    {
        var target = Path.Combine(_root, "overwrite");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "asset.txt"), "old");
        using var stream = Package("asset.txt");
        NuGetClient.ExtractNupkg(stream, target);
        File.ReadAllText(Path.Combine(target, "asset.txt")).ShouldEqual("replacement");
    }

    [Fact]
    void should_refuse_a_linked_entry_destination()
    {
        var target = Directory.CreateDirectory(Path.Combine(_root, "linked-entry")).FullName;
        var outside = Path.Combine(_root, "original.txt");
        File.WriteAllText(outside, "original");
        File.CreateSymbolicLink(Path.Combine(target, "asset.txt"), outside);
        using var stream = Package("asset.txt");
        Catch.Exception(() => NuGetClient.ExtractNupkg(stream, target)).ShouldBeOfExactType<TemplatePackageAcquisitionError>();
        File.ReadAllText(outside).ShouldEqual("original");
    }

    [Fact]
    void should_refuse_a_linked_extraction_root()
    {
        var outside = Directory.CreateDirectory(Path.Combine(_root, "outside")).FullName;
        var target = Path.Combine(_root, "linked-root");
        Directory.CreateSymbolicLink(target, outside);
        using var stream = Package("asset.txt");
        Catch.Exception(() => NuGetClient.ExtractNupkg(stream, target)).ShouldBeOfExactType<TemplatePackageAcquisitionError>();
        Directory.GetFileSystemEntries(outside).ShouldBeEmpty();
    }

    static MemoryStream Package(string name)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var writer = new StreamWriter(archive.CreateEntry(name).Open());
            writer.Write("replacement");
        }
        stream.Position = 0;
        return stream;
    }

    void Destroy() => Directory.Delete(_root, recursive: true);
}
