// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;

namespace Cratis.Cli.for_DesktopMcp;

public class when_extracting_an_escaping_archive : given.a_personal_marketplace
{
    string _unsafe;
    void Establish()
    {
        _unsafe = Path.Combine(_home, "unsafe.zip");
        using var archive = ZipFile.Open(_unsafe, ZipArchiveMode.Create);
        using var writer = new StreamWriter(archive.CreateEntry("../outside.txt").Open());
        writer.Write("escaped");
    }
    void Because() => _error = Catch.Exception(() => ChatGptDesktopMcp.Extract(_unsafe, Path.Combine(_home, "extract")));

    [Fact] void should_reject_zip_traversal() => _error.ShouldBeOfExactType<AiMcpConfigurationInvalid>();
    [Fact] void should_not_write_outside_the_source() => File.Exists(Path.Combine(_home, "outside.txt")).ShouldBeFalse();
}
