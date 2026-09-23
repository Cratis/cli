// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Expressions;

namespace Cratis.Templating.Specs.for_ExpressionEvaluator.given;

public abstract class an_evaluator : Specification
{
    protected static bool Evaluate(
        string expression,
        ExpressionDialect dialect = ExpressionDialect.Cpp2,
        IReadOnlyDictionary<string, string>? scope = null) =>
        ExpressionEvaluator.EvaluateBoolean(expression, dialect, scope ?? new Dictionary<string, string>());
}
