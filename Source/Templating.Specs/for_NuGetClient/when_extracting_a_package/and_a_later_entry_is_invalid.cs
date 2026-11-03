// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_NuGetClient.when_extracting_a_package;

public class and_a_later_entry_is_invalid : Specification
{
    string _root = null!;
    Exception? _error;

    void Establish() => _root = Path.Combine(Path.GetTempPath(), "cratis-extract-specs", Guid.NewGuid().ToString("N"));

    void Because()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var writer = new StreamWriter(archive.CreateEntry("first.txt").Open())) writer.Write("new");
            archive.CreateEntry("../other/last.txt");
        }
        stream.Position = 0;
        _error = Catch.Exception(() => NuGetClient.ExtractNupkg(stream, _root));
    }

    [Fact] void should_refuse_the_archive() => _error.ShouldBeOfExactType<TemplatePackageAcquisitionError>();
    [Fact] void should_not_write_the_first_entry() => File.Exists(Path.Combine(_root, "first.txt")).ShouldBeFalse();

    void Destroy()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
