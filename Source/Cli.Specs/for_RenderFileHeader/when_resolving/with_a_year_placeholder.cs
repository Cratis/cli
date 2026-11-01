// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderFileHeader.when_resolving;

public class with_a_year_placeholder : given.a_file_header
{
    Task Because() => Resolve(["Copyright (c) {year} Acme. Since {year}."]);

    [Fact] void should_replace_every_placeholder_with_the_year() => _result.Lines.Single().ShouldEqual("Copyright (c) 2031 Acme. Since 2031.");
}
