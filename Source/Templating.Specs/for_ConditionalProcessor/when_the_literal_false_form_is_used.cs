// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using Cratis.Templating.Expressions;
using Cratis.Templating.Processing;
using Cratis.Templating.Specs.for_ConditionalProcessor.given;

namespace Cratis.Templating.Specs.for_ConditionalProcessor;

public class when_the_literal_false_form_is_used : a_processor
{
    string _result = null!;

    void Because() => _result = Process("        #if (FALSE)\n        #if (SomeUndefinedSymbol)\n        removed verbatim\n        #endif\n        #endif\n        kept",
        LanguageConfig);

    [Fact] void should_remove_the_block_without_evaluating_nested_directives() => _result.ShouldNotContain("removed verbatim");
    [Fact] void should_keep_content_after_the_block() => _result.ShouldContain("kept");
}
