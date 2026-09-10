// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayProjectSelection.when_asking_what_a_compilation_can_declare;

public class and_it_resolves_neither_attribute : given.a_compilation_built_from_source
{
    bool _result;

    void Because() => _result = ScreenplayProjectSelection.CanDeclareAnArtifact(
        Holding("namespace MyApp.Analyzers { public class Rule { } }"));

    [Fact] void should_leave_it_out_of_the_application() => _result.ShouldBeFalse();
}
