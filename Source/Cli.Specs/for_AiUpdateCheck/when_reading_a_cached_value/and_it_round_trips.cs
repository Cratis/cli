// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiUpdateCheck.when_reading_a_cached_value;

public class and_it_round_trips : Specification
{
    AiCorpusUpdate? _result;

    void Because() => _result = AiUpdateCheck.FromCacheValue(AiUpdateCheck.ToCacheValue(new AiCorpusUpdate("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", 7)));

    [Fact] void should_read_back_the_comparison() => _result.ShouldEqual(new AiCorpusUpdate("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", 7));
}
