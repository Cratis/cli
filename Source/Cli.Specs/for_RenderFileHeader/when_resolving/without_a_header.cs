// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderFileHeader.when_resolving;

public class without_a_header : given.a_file_header
{
    Task Because() => Resolve([]);

    [Fact] void should_have_no_lines() => _result.Lines.ShouldBeEmpty();
    [Fact] void should_not_fail() => _result.Error.ShouldBeNull();
}
