// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderFileHeader.when_resolving;

public class with_line_breaks_in_a_value : given.a_file_header
{
    Task Because() => Resolve(["First\\nSecond", "Third\r\nFourth"]);

    [Fact] void should_split_on_escaped_and_real_line_breaks() => _result.Lines.SequenceEqual(["First", "Second", "Third", "Fourth"]).ShouldBeTrue();
}
