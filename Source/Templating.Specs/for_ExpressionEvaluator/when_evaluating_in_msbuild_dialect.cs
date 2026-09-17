// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Expressions;
using Cratis.Templating.Specs.for_ExpressionEvaluator.given;

namespace Cratis.Templating.Specs.for_ExpressionEvaluator;

public class when_evaluating_in_msbuild_dialect : an_evaluator
{
    readonly IReadOnlyDictionary<string, string> _scope = new Dictionary<string, string> { ["TargetFrameworkOverride"] = "net8.0" };

    [Fact] void should_use_single_quoted_strings_and_word_operators() =>
        Evaluate("'$(TargetFrameworkOverride)' == ''", ExpressionDialect.MSBuild, _scope).ShouldBeFalse();

    [Fact] void should_evaluate_empty_comparison_as_true_when_unset() =>
        Evaluate("'$(Missing)' == ''", ExpressionDialect.MSBuild, _scope).ShouldBeTrue();
}
