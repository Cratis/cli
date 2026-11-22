// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle.Observers;

namespace Cratis.Cli.for_ListObserversCommand.when_formatting_the_state_change_time;

public class and_it_is_unavailable : Specification
{
    string? _result;

    void Because() => _result = ListObserversCommand.FormatTimestamp(null);

    [Fact] void should_not_invent_a_time() => _result.ShouldBeNull();
}
