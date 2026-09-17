// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Expressions;
using Cratis.Templating.Specs.for_ExpressionEvaluator.given;

namespace Cratis.Templating.Specs.for_ExpressionEvaluator;

public class when_evaluating_quoteless_literals : an_evaluator
{
    readonly IReadOnlyDictionary<string, string> _scope = new Dictionary<string, string> { ["Framework"] = "net10.0" };
    readonly IReadOnlyCollection<string> _literals = ["net10.0", "net8.0"];

    [Fact] void should_resolve_unquoted_identifiers_as_symbols() =>
        ExpressionEvaluator.EvaluateBoolean("Framework == net10.0", ExpressionDialect.Cpp2, _scope, _literals).ShouldBeTrue();

    [Fact] void should_resolve_quoted_literals() => Evaluate("Framework == \"net10.0\"", scope: _scope).ShouldBeTrue();
}
