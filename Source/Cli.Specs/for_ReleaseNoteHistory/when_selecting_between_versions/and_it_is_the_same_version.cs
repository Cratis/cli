// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ReleaseNoteHistory.when_selecting_between_versions;

public class and_it_is_the_same_version : given.a_release_note_history
{
    [Fact] void should_select_nothing() => _history.Between("3.86.0", "v3.86.0").ShouldBeEmpty();
}
