// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayStatistics.when_counting;

public class and_the_document_routes_a_command : Specification
{
    ScreenplayStatistics _result = null!;

    void Because() => _result = ScreenplayStatistics.For(for_ScreenplayPlanning.when_planning_an_esm_v8_model.RoutedSource);

    [Fact] void should_count_the_module() => _result.Modules.ShouldEqual(1);
    [Fact] void should_count_the_feature() => _result.Features.ShouldEqual(1);
    [Fact] void should_count_the_slice() => _result.Slices.ShouldEqual(1);
    [Fact] void should_count_the_concepts() => _result.Concepts.ShouldEqual(2);
    [Fact] void should_count_the_routed_command() => _result.Commands.ShouldEqual(1);
}
