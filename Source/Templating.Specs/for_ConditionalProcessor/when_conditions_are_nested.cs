// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using Cratis.Templating.Expressions;
using Cratis.Templating.Processing;
using Cratis.Templating.Specs.for_ConditionalProcessor.given;

namespace Cratis.Templating.Specs.for_ConditionalProcessor;

public class when_conditions_are_nested : a_processor
{
    readonly IReadOnlyDictionary<string, string> _scope = new Dictionary<string, string> { ["A"] = "true", ["B"] = "false" };
    string _result = null!;

    void Because() => _result = Process("        #if (A)\n        outer\n        #if (B)\n        inner-b\n        #else\n        inner-else\n        #endif\n        #endif",
        LanguageConfig,
        _scope);

    [Fact] void should_evaluate_the_outer_branch() => _result.ShouldContain("outer");
    [Fact] void should_evaluate_the_nested_branch() => _result.ShouldContain("inner-else");
    [Fact] void should_remove_all_directives() => _result.ShouldNotContain("#if");
}
