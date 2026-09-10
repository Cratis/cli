// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayProjectSelection.when_asking_what_a_compilation_can_declare;

public class and_it_resolves_the_event_type_attribute : given.a_compilation_built_from_source
{
    bool _result;

    void Because() => _result = ScreenplayProjectSelection.CanDeclareAnArtifact(
        Holding("namespace Cratis.Chronicle.Events { public class EventTypeAttribute { } }"));

    [Fact] void should_take_it_for_part_of_the_application() => _result.ShouldBeTrue();
}
