// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Expressions;
using Cratis.Templating.Specs.for_ExpressionEvaluator.given;

namespace Cratis.Templating.Specs.for_ExpressionEvaluator;

public class when_operator_precedence_applies : an_evaluator
{
    readonly IReadOnlyDictionary<string, string> _scope = new Dictionary<string, string> { ["A"] = "true", ["B"] = "false", ["C"] = "true" };

    [Fact] void should_bind_and_tighter_than_or() => Evaluate("A || B && B", scope: _scope).ShouldBeTrue();
    [Fact] void should_honor_parentheses() => Evaluate("(A || B) && C", scope: _scope).ShouldBeTrue();
    [Fact] void should_negate_with_unary_not() => Evaluate("!B && A", scope: _scope).ShouldBeTrue();
}
