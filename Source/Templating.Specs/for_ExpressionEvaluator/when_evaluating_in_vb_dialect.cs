// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Expressions;
using Cratis.Templating.Specs.for_ExpressionEvaluator.given;

namespace Cratis.Templating.Specs.for_ExpressionEvaluator;

public class when_evaluating_in_vb_dialect : an_evaluator
{
    readonly IReadOnlyDictionary<string, string> _scope = new Dictionary<string, string> { ["Enabled"] = "true", ["Debug"] = "false" };

    [Fact] void should_use_vb_operators() => Evaluate("Enabled AndAlso Not Debug", ExpressionDialect.VB, _scope).ShouldBeTrue();
    [Fact] void should_use_vb_inequality() => Evaluate("Enabled <> Debug", ExpressionDialect.VB, _scope).ShouldBeTrue();
}
