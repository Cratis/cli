// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

using System.Globalization;

namespace Cratis.Templating.Expressions;

/// <summary>
/// Recursive-descent parser and evaluator. Precedence: Or, then And, then equality, then Not, then primary.
/// Multi-choice values (pipe-separated) use presence semantics: <c language="csharp">==</c> is satisfied when any of the
/// symbol's values equals the operand, and <c language="csharp">!=</c> when none does.
/// </summary>
/// <param name="tokens">The tokens to use.</param>
/// <param name="expression">The expression to use.</param>
/// <param name="dialect">The dialect to use.</param>
/// <param name="scope">The scope to use.</param>
/// <param name="knownLiterals">The opted-in choice values that evaluate as literals.</param>
sealed class Parser(IReadOnlyList<Token> tokens, string expression, ExpressionDialect dialect, IReadOnlyDictionary<string, string> scope, IReadOnlyCollection<string> knownLiterals)
{
    int _position;

    Token Current => tokens[_position];

    public string ParseOr()
    {
        var left = ParseAnd();
        while (Current.Kind == TokenKind.Or)
        {
            Advance();
            var right = ParseAnd();
            left = IsTruthy(left) || IsTruthy(right) ? bool.TrueString : bool.FalseString;
        }
        return left;
    }

    internal void ExpectEnd()
    {
        if (Current.Kind != TokenKind.End)
        {
            throw new ExpressionEvaluationError(expression, $"unexpected token '{Current.Text}' after expression.");
        }
    }

    static string[] SplitValues(string value) => value.Split('|', StringSplitOptions.RemoveEmptyEntries);

    static bool StringsEqual(string left, string right)
    {
        if (double.TryParse(left, CultureInfo.InvariantCulture, out var leftNumber)
            && double.TryParse(right, CultureInfo.InvariantCulture, out var rightNumber))
        {
            return Math.Abs(leftNumber - rightNumber) < double.Epsilon;
        }
        return string.Equals(left, right, StringComparison.Ordinal);
    }

    static bool IsTruthy(string value) => ExpressionEvaluator.IsTruthy(value);

    string ParseAnd()
    {
        var left = ParseEquality();
        while (Current.Kind == TokenKind.And)
        {
            Advance();
            var right = ParseEquality();
            left = IsTruthy(left) && IsTruthy(right) ? bool.TrueString : bool.FalseString;
        }
        return left;
    }

    string ParseEquality()
    {
        var left = ParseUnary();
        while (Current.Kind is TokenKind.Equal or TokenKind.NotEqual)
        {
            var isNotEqual = Current.Kind == TokenKind.NotEqual;
            Advance();
            var right = ParseUnary();
            var equal = ValuesEqual(left, right);
            left = (isNotEqual ? !equal : equal) ? bool.TrueString : bool.FalseString;
        }
        return left;
    }

    string ParseUnary()
    {
        if (Current.Kind == TokenKind.Not)
        {
            Advance();
            return IsTruthy(ParseUnary()) ? bool.FalseString : bool.TrueString;
        }
        return ParsePrimary();
    }

    string ParsePrimary()
    {
        var token = Current;
        switch (token.Kind)
        {
            case TokenKind.String:
                Advance();

                // MSBuild semantics: property references expand inside quoted literals, so
                // '$(X)' yields the value of X quoted — and the empty string when X is undefined.
                return dialect == ExpressionDialect.MSBuild ? ExpandVariableReferences(token.Text) : token.Text;

            case TokenKind.Number:
                Advance();
                return token.Text;

            case TokenKind.OpenParen:
                Advance();
                var inner = ParseOr();
                if (Current.Kind != TokenKind.CloseParen)
                {
                    throw new ExpressionEvaluationError(expression, "missing closing parenthesis.");
                }
                Advance();
                return inner;

            case TokenKind.Variable:
                Advance();

                // A $() reference resolves to empty when the symbol is not defined — MSBuild semantics.
                return scope.TryGetValue(token.Text, out var referenced) ? referenced : string.Empty;

            case TokenKind.Identifier:
                Advance();
                if (token.Text.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase)
                    || token.Text.Equals(bool.FalseString, StringComparison.OrdinalIgnoreCase))
                {
                    return token.Text.ToLowerInvariant();
                }
                if (scope.TryGetValue(token.Text, out var value))
                {
                    return value;
                }

                // An identifier that names an opted-in choice value is a quoteless literal — the
                // documented opt-in. Anything else is an unresolved symbol and evaluates empty,
                // so a plain '#if (Symbol)' on an undefined symbol is false.
                return knownLiterals.Contains(token.Text) ? token.Text : string.Empty;

            case TokenKind.End:
                throw new ExpressionEvaluationError(expression, "unexpected end of expression.");

            default:
                throw new ExpressionEvaluationError(expression, $"unexpected token '{token.Text}'.");
        }
    }

    string ExpandVariableReferences(string value)
    {
        if (!value.Contains("$(", StringComparison.Ordinal))
        {
            return value;
        }

        var result = value;
        var search = 0;
        while (result.IndexOf("$(", search, StringComparison.Ordinal) is var start && start >= 0)
        {
            var end = result.IndexOf(')', start);
            if (end < 0)
            {
                break;
            }

            var name = result[(start + 2)..end];
            var replacement = scope.TryGetValue(name, out var referenced) ? referenced : string.Empty;
            result = result[..start] + replacement + result[(end + 1)..];
            search = start + replacement.Length;
        }
        return result;
    }

    bool ValuesEqual(string left, string right)
    {
        var leftValues = SplitValues(left);
        var rightValues = SplitValues(right);
        if (leftValues.Length > 1 && rightValues.Length == 1)
        {
            // Multi-choice compared to single: presence semantics.
            return leftValues.Contains(rightValues[0], StringComparer.Ordinal);
        }
        if (rightValues.Length > 1 && leftValues.Length == 1)
        {
            return rightValues.Contains(leftValues[0], StringComparer.Ordinal);
        }
        if (leftValues.Length > 1 && rightValues.Length > 1)
        {
            return leftValues.SequenceEqual(rightValues, StringComparer.Ordinal);
        }
        return StringsEqual(left, right);
    }

    void Advance() => _position++;
}
