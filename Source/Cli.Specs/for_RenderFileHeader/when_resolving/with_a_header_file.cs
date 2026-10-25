// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderFileHeader.when_resolving;

public class with_a_header_file : given.a_file_header
{
    async Task Because()
    {
        var file = Path.Combine(_folder, "header.txt");
        await File.WriteAllTextAsync(file, "Copyright (c) {year} Acme.\r\n\r\nLicensed under the Acme license.\n");
        await Resolve([], file);
    }

    [Fact] void should_read_one_line_per_line_without_the_final_line_break() => _result.Lines.SequenceEqual(["Copyright (c) 2031 Acme.", "", "Licensed under the Acme license."]).ShouldBeTrue();
    [Fact] void should_not_fail() => _result.Error.ShouldBeNull();
}
