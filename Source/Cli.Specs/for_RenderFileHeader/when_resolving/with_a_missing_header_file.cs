// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderFileHeader.when_resolving;

public class with_a_missing_header_file : given.a_file_header
{
    Task Because() => Resolve([], Path.Combine(_folder, "missing.txt"));

    [Fact] void should_report_it_as_not_found() => _result.NotFound.ShouldBeTrue();
    [Fact] void should_explain_why() => _result.Error.ShouldEqual("The file header file was not found");
    [Fact] void should_have_no_lines() => _result.Lines.ShouldBeEmpty();
}
