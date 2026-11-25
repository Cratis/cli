// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected.for_CoberturaReader;

public class when_reading_a_report : Specification
{
    IReadOnlySet<string> _files;

    void Because()
    {
        var xml = string.Concat(
            "<coverage><sources><source>/repo/</source></sources><packages><package><classes>",
            "<class name=\"A\" filename=\"Source/Foo/A.cs\"><lines><line number=\"3\" hits=\"4\" /></lines></class>",
            "<class name=\"A2\" filename=\"/repo/Source/Foo/A.cs\"><lines><line number=\"9\" hits=\"1\" /></lines></class>",
            "<class name=\"B\" filename=\"/repo/Source/Foo/B.cs\"><lines><line number=\"3\" hits=\"0\" /></lines></class>",
            "<class name=\"G\" filename=\"/repo/Source/Foo/obj/Generated.cs\"><lines><line number=\"3\" hits=\"2\" /></lines></class>",
            "<class name=\"C\" filename=\"/elsewhere/nuget/Lib.cs\"><lines><line number=\"3\" hits=\"2\" /></lines></class>",
            "</classes></package></packages></coverage>");
        _files = CoberturaReader.Files(xml, "/repo", new HashSet<string> { "Source/Foo/A.cs", "Source/Foo/B.cs" });
    }

    [Fact] void should_return_tracked_repository_files_that_ran_once() => _files.ShouldContainOnly("Source/Foo/A.cs");
    [Fact] void should_not_return_a_file_whose_lines_never_ran() => _files.ShouldNotContain("Source/Foo/B.cs");
}
