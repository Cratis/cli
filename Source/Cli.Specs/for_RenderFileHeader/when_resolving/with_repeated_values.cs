// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderFileHeader.when_resolving;

public class with_repeated_values : given.a_file_header
{
    Task Because() => Resolve(["Copyright (c) Acme.", "", "Proprietary and confidential."]);

    [Fact] void should_keep_every_line_in_order() => _result.Lines.SequenceEqual(["Copyright (c) Acme.", "", "Proprietary and confidential."]).ShouldBeTrue();
    [Fact] void should_not_fail() => _result.Error.ShouldBeNull();
}
