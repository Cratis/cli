// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CachedVersionCheck.when_choosing_a_backoff;

/// <summary>
/// An unauthenticated GitHub rate limit resets within the hour, so asking sooner only spends it again.
/// </summary>
public class and_the_source_was_rate_limited : Specification
{
    TimeSpan _result;

    void Because() => _result = CachedVersionCheck.BackoffFor(true);

    [Fact] void should_wait_until_the_limit_resets() => _result.ShouldEqual(TimeSpan.FromHours(1));
}
