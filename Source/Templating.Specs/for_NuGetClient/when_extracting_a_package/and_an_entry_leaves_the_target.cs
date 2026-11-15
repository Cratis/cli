// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using Cratis.Templating.Packages;

namespace Cratis.Templating.Specs.for_NuGetClient.when_extracting_a_package;

public class and_an_entry_leaves_the_target : Specification
{
    string _target;
    string _outsideFile;
    MemoryStream _package;
    Exception? _error;

    void Establish()
    {
        var parent = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-extract", Guid.NewGuid().ToString("N"))).FullName;
        _target = Path.Combine(parent, "app");
        _outsideFile = Path.Combine(parent, "app-evil", "x.txt");
        _package = new MemoryStream();
        using (var archive = new ZipArchive(_package, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var writer = new StreamWriter(archive.CreateEntry("../app-evil/x.txt").Open());
            writer.Write("escaped");
        }
        _package.Position = 0;
    }

    void Because() => _error = Catch.Exception(() => NuGetClient.ExtractNupkg(_package, _target));

    [Fact] void should_refuse_the_package() => _error.ShouldBeOfExactType<TemplatePackageAcquisitionError>();
    [Fact] void should_not_write_the_sibling_file() => File.Exists(_outsideFile).ShouldBeFalse();
}
