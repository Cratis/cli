// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_reading_a_cached_value;

public class and_the_count_is_negative : Specification
{
    AiCorpusUpdate? _result;

    void Because() => _result = AiUpdateCheck.FromCacheValue("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa:-1");

    [Fact] void should_not_read_a_comparison() => _result.ShouldBeNull();
}
