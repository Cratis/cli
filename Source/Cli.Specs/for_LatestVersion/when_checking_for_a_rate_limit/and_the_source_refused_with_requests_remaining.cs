// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LatestVersion.when_checking_for_a_rate_limit;

public class and_the_source_refused_with_requests_remaining : Specification
{
    bool _result;

    void Because() => _result = LatestVersion.IsRateLimited(System.Net.HttpStatusCode.Forbidden, "12");

    [Fact] void should_not_be_rate_limited() => _result.ShouldBeFalse();
}
