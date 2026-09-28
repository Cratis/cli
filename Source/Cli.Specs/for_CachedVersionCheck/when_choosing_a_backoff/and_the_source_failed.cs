// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CachedVersionCheck.when_choosing_a_backoff;

public class and_the_source_failed : Specification
{
    TimeSpan _result;

    void Because() => _result = CachedVersionCheck.BackoffFor(false);

    [Fact] void should_wait_a_quarter_of_an_hour() => _result.ShouldEqual(TimeSpan.FromMinutes(15));
}
