// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNotesCommand;

public class when_selecting_one_unknown_release : Specification
{
    [Fact] void should_select_nothing() => ReleaseNotesCommand.Select(new([new("3.86.0", "c")]), "3.86.0", new() { Version = "9.9.9" }).ShouldBeNull();
}
