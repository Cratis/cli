// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Expressions;
using Cratis.Templating.Specs.for_ExpressionEvaluator.given;

namespace Cratis.Templating.Specs.for_ExpressionEvaluator;

public class when_evaluating_multichoice_symbols : an_evaluator
{
    readonly IReadOnlyDictionary<string, string> _scope = new Dictionary<string, string>
    {
        ["Platform"] = "MacOS|iOS"
    };

    [Fact] void should_treat_equality_as_contains_for_multichoice() => Evaluate("Platform == \"MacOS\"", scope: _scope).ShouldBeTrue();
    [Fact] void should_treat_equality_as_contains_for_either_value() => Evaluate("Platform == \"iOS\"", scope: _scope).ShouldBeTrue();
    [Fact] void should_treat_inequality_as_not_containing() => Evaluate("Platform != \"android\"", scope: _scope).ShouldBeTrue();
    [Fact] void should_not_match_a_value_that_is_absent() => Evaluate("Platform == \"Windows\"", scope: _scope).ShouldBeFalse();
    [Fact] void should_allow_operand_order_to_be_reversed() => Evaluate("\"MacOS\" == Platform", scope: _scope).ShouldBeTrue();
}
