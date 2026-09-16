// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using Cratis.Templating.Expressions;
using Cratis.Templating.Processing;
using Cratis.Templating.Specs.for_ConditionalProcessor.given;

namespace Cratis.Templating.Specs.for_ConditionalProcessor;

public class when_the_directive_is_actionable : a_processor
{
    readonly IReadOnlyDictionary<string, string> _scope = new Dictionary<string, string> { ["param2"] = "true" };
    string _result = null!;

    void Because() => _result = Process("        //#if (param1)\n        // comment related to the 'if' content\n        default content\n        ////#elseif (param2)\n        //// comment related to the 'elseif' content\n        //content for when param2 is true and param1 is false\n        ////#else\n        //// comment related to the 'else' content\n        // content for when both param1 & param2 are false\n        //#endif",
        JsonConfig,
        _scope);

    [Fact] void should_remove_the_directive_lines() => _result.ShouldNotContain("#if");
    [Fact] void should_keep_the_body_of_the_taken_branch() => _result.ShouldContain("content for when param2 is true and param1 is false");
    [Fact] void should_remove_the_bodies_of_untaken_branches() => _result.ShouldNotContain("default content");
    [Fact] void should_uncomment_the_actionable_body() => _result.ShouldContain("comment related to the 'elseif' content");
}
