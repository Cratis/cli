// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderFileHeader.when_resolving;

public class with_values_and_a_header_file : given.a_file_header
{
    Task Because() => Resolve(["Acme"], Path.Combine(_folder, "header.txt"));

    [Fact] void should_refuse_the_combination() => _result.Error.ShouldEqual("--file-header and --file-header-file are mutually exclusive");
    [Fact] void should_not_report_a_missing_file() => _result.NotFound.ShouldBeFalse();
}
