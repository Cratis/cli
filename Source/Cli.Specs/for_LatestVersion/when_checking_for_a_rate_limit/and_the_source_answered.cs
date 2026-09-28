// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LatestVersion.when_checking_for_a_rate_limit;

/// <summary>
/// The last request the limit allows still carries its answer.
/// </summary>
public class and_the_source_answered : Specification
{
    bool _result;

    void Because() => _result = LatestVersion.IsRateLimited(System.Net.HttpStatusCode.OK, "0");

    [Fact] void should_not_be_rate_limited() => _result.ShouldBeFalse();
}
