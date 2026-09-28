// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LatestVersion.when_checking_for_a_rate_limit;

public class and_the_source_answered_too_many_requests : Specification
{
    bool _result;

    void Because() => _result = LatestVersion.IsRateLimited(System.Net.HttpStatusCode.TooManyRequests, null);

    [Fact] void should_be_rate_limited() => _result.ShouldBeTrue();
}
