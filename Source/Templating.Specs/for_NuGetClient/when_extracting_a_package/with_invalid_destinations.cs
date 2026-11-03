// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_NuGetClient.when_extracting_a_package;

public class with_invalid_destinations : Specification
{
    string _root = null!;

    void Establish() => _root = Path.Combine(Path.GetTempPath(), "cratis-entry-specs", Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData("../target-sibling/out.txt")]
    [InlineData("..\\target-sibling\\out.txt")]
    [InlineData("/absolute.txt")]
    [InlineData("C:\\absolute.txt")]
    [InlineData("\\\\host\\share\\file")]
    [InlineData(".cratis-complete")]
    [InlineData(".cratis-stage-other/file")]
    [InlineData("_rels/../../file")]
    [InlineData("folder/./file")]
    [InlineData("folder//file")]
    [InlineData("file:stream")]
    [InlineData("file.")]
    [InlineData("NUL.txt")]
    void should_preflight_every_entry(string name)
    {
        using var stream = Archive("first.txt", name);
        var error = Catch.Exception(() => NuGetClient.ExtractNupkg(stream, _root));
        error.ShouldBeOfExactType<TemplatePackageAcquisitionError>();
        Directory.Exists(_root).ShouldBeFalse();
    }

    [Theory]
    [InlineData("same.txt", "same.txt")]
    [InlineData("folder/file.txt", "folder\\file.txt")]
    [InlineData("file", "file/child.txt")]
    [InlineData("file/child.txt", "file")]
    void should_refuse_colliding_destinations(string first, string second)
    {
        using var stream = Archive(first, second);
        Catch.Exception(() => NuGetClient.ExtractNupkg(stream, _root)).ShouldBeOfExactType<TemplatePackageAcquisitionError>();
        Directory.Exists(_root).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_link_entries_before_writing()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            archive.CreateEntry("first.txt");
            archive.CreateEntry("link").ExternalAttributes = (0xa000 | 0x1ff) << 16;
        }
        stream.Position = 0;
        Catch.Exception(() => NuGetClient.ExtractNupkg(stream, _root)).ShouldBeOfExactType<TemplatePackageAcquisitionError>();
        Directory.Exists(_root).ShouldBeFalse();
    }

    static MemoryStream Archive(string first, string second)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            archive.CreateEntry(first);
            archive.CreateEntry(second);
        }
        stream.Position = 0;
        return stream;
    }

    void Destroy()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
