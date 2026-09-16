// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

namespace Cratis.Templating.Expressions;

/// <summary>
/// The supported condition expression evaluator dialects.
/// </summary>
public enum ExpressionDialect
{
    /// <summary>The C++ preprocessor style: ==, !=, &amp;&amp;, ||, !, with C++ legacy unresolved-symbol behavior.</summary>
    Cpp,

    /// <summary>The C++ style with current semantics: unresolved symbols evaluate as empty rather than erroring. Default.</summary>
    Cpp2,

    /// <summary>The MSBuild style: ==, !=, And, Or, Not, single-quoted strings.</summary>
    MSBuild,

    /// <summary>The Visual Basic style: =, &lt;&gt;, AndAlso, OrElse, Not.</summary>
    VB
}

/// <summary>
/// Parses and evaluates condition expressions for all four documented dialects. Expressions are composed of
/// constant literals (strings, numbers, true, false), operators, symbols, brackets and whitespace. Boolean and
/// numeric expressions are supported — nonzero is <c language="csharp">true</c>. Comparison against a multi-choice symbol checks
/// for the presence of any matching value, so <c language="csharp">==</c> behaves as <c language="csharp">contains</c>.
/// </summary>
public static class ExpressionEvaluator
{
    /// <summary>
    /// Evaluates an expression to its boolean truth.
    /// </summary>
    /// <param name="expression">The expression text.</param>
    /// <param name="dialect">The evaluator dialect.</param>
    /// <param name="scope">Symbol values keyed by symbol name.</param>
    /// <returns>The boolean result.</returns>
    public static bool EvaluateBoolean(string expression, ExpressionDialect dialect, IReadOnlyDictionary<string, string> scope) =>
        EvaluateBoolean(expression, dialect, scope, []);

    /// <summary>
    /// Evaluates an expression to its boolean truth, with a set of known quoteless choice literals —
    /// identifiers that opted-in choice parameters accept and that therefore evaluate as string
    /// literals rather than unresolved symbols.
    /// </summary>
    /// <param name="expression">The expression text.</param>
    /// <param name="dialect">The evaluator dialect.</param>
    /// <param name="scope">Symbol values keyed by symbol name.</param>
    /// <param name="knownLiterals">The opted-in choice values that evaluate as literals.</param>
    /// <returns>The boolean result.</returns>
    public static bool EvaluateBoolean(
        string expression,
        ExpressionDialect dialect,
        IReadOnlyDictionary<string, string> scope,
        IReadOnlyCollection<string> knownLiterals)
    {
        var value = Evaluate(expression, dialect, scope, knownLiterals);
        return IsTruthy(value);
    }

    /// <summary>
    /// Evaluates an expression to its string value.
    /// </summary>
    /// <param name="expression">The expression text.</param>
    /// <param name="dialect">The evaluator dialect.</param>
    /// <param name="scope">Symbol values keyed by symbol name.</param>
    /// <returns>The string result.</returns>
    public static string Evaluate(string expression, ExpressionDialect dialect, IReadOnlyDictionary<string, string> scope) =>
        Evaluate(expression, dialect, scope, []);

    /// <summary>
    /// Evaluates an expression to its string value, with known quoteless choice literals.
    /// </summary>
    /// <param name="expression">The expression text.</param>
    /// <param name="dialect">The evaluator dialect.</param>
    /// <param name="scope">Symbol values keyed by symbol name.</param>
    /// <param name="knownLiterals">The opted-in choice values that evaluate as literals.</param>
    /// <returns>The string result.</returns>
    public static string Evaluate(
        string expression,
        ExpressionDialect dialect,
        IReadOnlyDictionary<string, string> scope,
        IReadOnlyCollection<string> knownLiterals)
    {
        var tokens = Tokenizer.Tokenize(expression, dialect);
        var parser = new Parser(tokens, expression, dialect, scope, knownLiterals);
        var result = parser.ParseOr();
        parser.ExpectEnd();
        return result;
    }

    /// <summary>
    /// Resolves a dialect from its template.json name.
    /// </summary>
    /// <param name="name">The dialect name: C++, C++2, MSBUILD or VB.</param>
    /// <returns>The dialect.</returns>
    /// <exception cref="ExpressionEvaluationError">Thrown when the expression cannot be evaluated.</exception>
    public static ExpressionDialect DialectFromName(string? name) => (name ?? "C++2").ToUpperInvariant() switch
    {
        "C++" => ExpressionDialect.Cpp,
        "C++2" => ExpressionDialect.Cpp2,
        "MSBUILD" => ExpressionDialect.MSBuild,
        "VB" => ExpressionDialect.VB,
        _ => throw new ExpressionEvaluationError(name ?? string.Empty, $"unknown evaluator '{name}'. Known evaluators: C++, C++2, MSBUILD, VB.")
    };

    /// <summary>
    /// Determines whether a string value is truthy: true, a nonzero number, or a non-empty non-false string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>True when the value is truthy.</returns>
    internal static bool IsTruthy(string value)
    {
        if (value.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (value.Equals(bool.FalseString, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var number)
            ? number != 0
            : value.Length > 0;
    }
}
