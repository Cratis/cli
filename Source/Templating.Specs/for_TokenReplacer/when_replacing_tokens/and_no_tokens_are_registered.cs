// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Processing;

namespace Cratis.Templating.Specs.for_TokenReplacer.when_replacing_tokens;

public class and_no_tokens_are_registered : Specification
{
    string _content = "unchanged";

    [Fact] void should_return_the_content_verbatim() =>
        new TokenReplacer().Replace(_content).ShouldEqual("unchanged");
}
