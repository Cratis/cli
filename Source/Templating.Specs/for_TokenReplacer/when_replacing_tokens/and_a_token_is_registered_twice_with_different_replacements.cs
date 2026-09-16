// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Processing;

namespace Cratis.Templating.Specs.for_TokenReplacer.when_replacing_tokens;

public class and_a_token_is_registered_twice_with_different_replacements : Specification
{
    Exception? _error;

    void Because()
    {
        var replacer = new TokenReplacer();
        replacer.Add("SAME", "first");
        _error = Catch.Exception(() => replacer.Add("SAME", "second"));
    }

    [Fact] void should_fail_loudly() => _error.ShouldNotBeNull();
}
