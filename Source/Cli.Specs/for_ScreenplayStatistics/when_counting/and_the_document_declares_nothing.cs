// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayStatistics.when_counting;

public class and_the_document_declares_nothing : Specification
{
    const string Source = "domain Library\n\nmodule Library\n";

    ScreenplayStatistics _result;

    void Because() => _result = ScreenplayStatistics.For(Source);

    [Fact] void should_count_the_one_module() => _result.Modules.ShouldEqual(1);
    [Fact] void should_count_no_features() => _result.Features.ShouldEqual(0);
    [Fact] void should_count_no_slices() => _result.Slices.ShouldEqual(0);
    [Fact] void should_count_no_commands() => _result.Commands.ShouldEqual(0);
    [Fact] void should_count_no_events() => _result.Events.ShouldEqual(0);
}
