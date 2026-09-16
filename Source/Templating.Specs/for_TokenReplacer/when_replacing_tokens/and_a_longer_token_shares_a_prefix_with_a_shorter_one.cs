// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Processing;

namespace Cratis.Templating.Specs.for_TokenReplacer.when_replacing_tokens;

public class and_a_longer_token_shares_a_prefix_with_a_shorter_one : Specification
{
    string _result = null!;

    void Establish()
    {
        var replacer = new TokenReplacer();
        replacer.Add("NAME", "short");
        replacer.Add("NAME_SUFFIX", "longer");
        _result = replacer.Replace("NAME_SUFFIX then NAME");
    }

    [Fact] void should_prefer_the_longest_match_at_each_position() => _result.ShouldEqual("longer then short");
}
