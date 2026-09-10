// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayStatistics.when_counting;

public class and_the_source_does_not_compile : Specification
{
    const string Source = "domain Library\n\nmodule Library\n  feature Lending\n    slice Reserving\n";

    ScreenplayStatistics _result;

    void Because() => _result = ScreenplayStatistics.For(Source);

    [Fact] void should_fall_back_to_zero() => _result.ShouldEqual(ScreenplayStatistics.Zero);
}
