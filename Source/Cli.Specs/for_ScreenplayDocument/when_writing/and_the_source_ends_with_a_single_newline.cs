// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_ScreenplayDocument.when_writing;

public class and_the_source_ends_with_a_single_newline : Specification
{
    const string Source = "domain Library\n\nmodule Library\n";
    byte[] _written;

    async Task Because()
    {
        await using var stream = new MemoryStream();
        await ScreenplayDocument.Write(stream, Source, CancellationToken.None);
        _written = stream.ToArray();
    }

    [Fact] void should_write_the_source_byte_for_byte() => _written.ShouldEqual(Encoding.UTF8.GetBytes(Source));
    [Fact] void should_not_emit_a_byte_order_mark() => _written[0].ShouldEqual((byte)'d');
    [Fact] void should_end_with_exactly_one_newline() => _written[^1].ShouldEqual((byte)'\n');
    [Fact] void should_not_end_with_two_newlines() => _written[^2].ShouldNotEqual((byte)'\n');
}
