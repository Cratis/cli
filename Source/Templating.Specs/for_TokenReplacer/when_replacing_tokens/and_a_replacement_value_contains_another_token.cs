// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Processing;

namespace Cratis.Templating.Specs.for_TokenReplacer.when_replacing_tokens;

public class and_a_replacement_value_contains_another_token : Specification
{
    string _result = null!;

    void Establish()
    {
        var replacer = new TokenReplacer();
        replacer.Add("OUTER", "INNER");
        replacer.Add("INNER", "must not be applied");
        _result = replacer.Replace("OUTER");
    }

    [Fact] void should_not_re_replace_replaced_content() => _result.ShouldEqual("INNER");
}
