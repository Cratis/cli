// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_ScreenplayDocument.when_writing;

public class and_the_source_holds_non_ascii_characters : Specification
{
    const string Source = "domain Bibliothèque\n\nmodule Utlån\n";
    byte[] _written;

    async Task Because()
    {
        await using var stream = new MemoryStream();
        await ScreenplayDocument.Write(stream, Source, CancellationToken.None);
        _written = stream.ToArray();
    }

    [Fact] void should_write_them_as_utf8() => _written.ShouldEqual(Encoding.UTF8.GetBytes(Source));
}
