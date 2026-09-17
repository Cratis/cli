// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using Cratis.Templating.Expressions;
using Cratis.Templating.Processing;
using Cratis.Templating.Specs.for_ConditionalProcessor.given;

namespace Cratis.Templating.Specs.for_ConditionalProcessor;

public class when_processing_is_disabled_for_a_region : a_processor
{
    string _result = null!;

    void Because() => _result = Process("        #if (DEBUG)\n        removed\n        #endif\n        //-:cnd:noEmit\n        #if (DEBUG)\n        kept verbatim\n        #endif\n        //+:cnd:noEmit",
        LanguageConfig);

    [Fact]
    void should_remove_processed_conditionals() => _result.ShouldNotContain("removed");

    [Fact]
    void should_keep_the_disabled_region_verbatim() => _result.ShouldContain("#if (DEBUG)");

    [Fact]
    void should_remove_the_marker_lines() => _result.ShouldNotContain("cnd:noEmit");
}
