// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Processing;

namespace Cratis.Templating.Specs.for_TokenReplacer.when_replacing_tokens;

public class and_content_has_no_matching_tokens : Specification
{
    string _result = null!;

    void Establish()
    {
        var replacer = new TokenReplacer();
        replacer.Add("TOKEN", "value");
        _result = replacer.Replace("nothing to see here");
    }

    [Fact] void should_return_the_content_unchanged() => _result.ShouldEqual("nothing to see here");

    [Fact] void should_report_no_tokens_present() => new TokenReplacer().ContainsAnyToken("nothing to see here").ShouldBeFalse();
}
