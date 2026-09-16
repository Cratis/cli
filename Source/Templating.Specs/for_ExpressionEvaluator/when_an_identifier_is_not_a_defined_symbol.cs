// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Expressions;
using Cratis.Templating.Specs.for_ExpressionEvaluator.given;

namespace Cratis.Templating.Specs.for_ExpressionEvaluator;

public class when_an_identifier_is_not_a_defined_symbol : an_evaluator
{
    [Fact] void should_fall_back_to_a_quoteless_literal_when_opted_in() =>
        ExpressionEvaluator.EvaluateBoolean("Framework == net10.0", ExpressionDialect.Cpp2, new Dictionary<string, string> { ["Framework"] = "net10.0" }, ["net10.0"]).ShouldBeTrue();

    [Fact] void should_evaluate_an_undefined_symbol_as_falsy() => Evaluate("Undefined").ShouldBeFalse();

    [Fact] void should_resolve_a_variable_reference_to_empty_when_undefined() => Evaluate("$(Missing) == ''", ExpressionDialect.MSBuild).ShouldBeTrue();

    [Fact] void should_resolve_a_variable_reference_to_its_value_when_defined() =>
        Evaluate("$(T) == 'x'", ExpressionDialect.MSBuild, new Dictionary<string, string> { ["T"] = "x" }).ShouldBeTrue();
}
