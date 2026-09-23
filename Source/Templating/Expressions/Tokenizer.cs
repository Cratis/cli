// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

namespace Cratis.Templating.Expressions;

/// <summary>
/// The kinds of tokens produced by the expression tokenizer.
/// </summary>
enum TokenKind
{
    /// <summary>A string literal, quoted per dialect.</summary>
    String,

    /// <summary>A numeric literal.</summary>
    Number,

    /// <summary>An identifier — a symbol name or quoteless choice literal.</summary>
    Identifier,

    /// <summary>A variable reference in <c language="csharp">$(name)</c> form, which resolves to empty when undefined.</summary>
    Variable,

    /// <summary>The equality operator for the dialect.</summary>
    Equal,

    /// <summary>The inequality operator for the dialect.</summary>
    NotEqual,

    /// <summary>The logical and operator for the dialect.</summary>
    And,

    /// <summary>The logical or operator for the dialect.</summary>
    Or,

    /// <summary>The logical not operator for the dialect.</summary>
    Not,

    /// <summary>An opening parenthesis.</summary>
    OpenParen,

    /// <summary>A closing parenthesis.</summary>
    CloseParen,

    /// <summary>End of expression.</summary>
    End
}

/// <summary>
/// A single token with its text.
/// </summary>
/// <param name="Kind">The token kind.</param>
/// <param name="Text">The token text.</param>
sealed record Token(TokenKind Kind, string Text);

/// <summary>
/// Tokenizes condition expressions per dialect. C++ and C++2 use <c language="csharp">== != &amp;&amp; || !</c> with double-quoted
/// strings; MSBuild uses <c language="csharp">== != And Or Not</c> with single-quoted strings; VB uses <c language="csharp">= &lt;&gt; AndAlso OrElse Not</c>.
/// </summary>
static class Tokenizer
{
    /// <summary>
    /// Tokenizes an expression for a dialect.
    /// </summary>
    /// <param name="expression">The expression text.</param>
    /// <param name="dialect">The dialect.</param>
    /// <returns>The tokens, terminated by an End token.</returns>
    /// <exception cref="ExpressionEvaluationError">Thrown when the expression cannot be evaluated.</exception>
    public static IReadOnlyList<Token> Tokenize(string expression, ExpressionDialect dialect)
    {
        var tokens = new List<Token>();
        var position = 0;
        while (position < expression.Length)
        {
            var character = expression[position];

            if (char.IsWhiteSpace(character))
            {
                position++;
                continue;
            }

            if (character == '(')
            {
                tokens.Add(new Token(TokenKind.OpenParen, "("));
                position++;
                continue;
            }

            if (character == ')')
            {
                tokens.Add(new Token(TokenKind.CloseParen, ")"));
                position++;
                continue;
            }

            if (dialect is ExpressionDialect.MSBuild or ExpressionDialect.Cpp or ExpressionDialect.Cpp2
                && character == '=' && Peek(expression, position + 1) == '=')
            {
                tokens.Add(new Token(TokenKind.Equal, "=="));
                position += 2;
                continue;
            }

            if (dialect == ExpressionDialect.VB && character == '=' && Peek(expression, position + 1) != '=')
            {
                tokens.Add(new Token(TokenKind.Equal, "="));
                position++;
                continue;
            }

            if (dialect is ExpressionDialect.MSBuild or ExpressionDialect.Cpp or ExpressionDialect.Cpp2
                && character == '!' && Peek(expression, position + 1) == '=')
            {
                tokens.Add(new Token(TokenKind.NotEqual, "!="));
                position += 2;
                continue;
            }

            if (dialect == ExpressionDialect.VB && character == '<' && Peek(expression, position + 1) == '>')
            {
                tokens.Add(new Token(TokenKind.NotEqual, "<>"));
                position += 2;
                continue;
            }

            if (dialect is ExpressionDialect.Cpp or ExpressionDialect.Cpp2 && character == '&' && Peek(expression, position + 1) == '&')
            {
                tokens.Add(new Token(TokenKind.And, "&&"));
                position += 2;
                continue;
            }

            if (dialect is ExpressionDialect.Cpp or ExpressionDialect.Cpp2 && character == '|' && Peek(expression, position + 1) == '|')
            {
                tokens.Add(new Token(TokenKind.Or, "||"));
                position += 2;
                continue;
            }

            if (dialect is ExpressionDialect.Cpp or ExpressionDialect.Cpp2 && character == '!')
            {
                tokens.Add(new Token(TokenKind.Not, "!"));
                position++;
                continue;
            }

            var quote = dialect == ExpressionDialect.MSBuild ? '\'' : '"';
            if (character == quote)
            {
                var end = expression.IndexOf(quote, position + 1);
                if (end < 0)
                {
                    throw new ExpressionEvaluationError(expression, "unterminated string literal.");
                }
                tokens.Add(new Token(TokenKind.String, expression[(position + 1)..end]));
                position = end + 1;
                continue;
            }

            if (char.IsDigit(character) || (character == '-' && char.IsDigit(Peek(expression, position + 1) ?? '0')))
            {
                var end = position + 1;
                while (end < expression.Length && (char.IsDigit(expression[end]) || expression[end] == '.' || expression[end] == '-' || expression[end] == '+'))
                {
                    end++;
                }
                tokens.Add(new Token(TokenKind.Number, expression[position..end]));
                position = end;
                continue;
            }

            // MSBuild-style variable reference: $(name) resolves from the scope.
            if (character == '$' && Peek(expression, position + 1) == '(')
            {
                var close = expression.IndexOf(')', position + 2);
                if (close < 0)
                {
                    throw new ExpressionEvaluationError(expression, "unterminated $() reference.");
                }
                tokens.Add(new Token(TokenKind.Variable, expression[(position + 2)..close]));
                position = close + 1;
                continue;
            }

            if (char.IsLetter(character) || character == '_')
            {
                var end = position + 1;

                // Dots are part of identifiers so quoteless choice literals like net10.0 tokenize whole.
                while (end < expression.Length && (char.IsLetterOrDigit(expression[end]) || expression[end] == '_' || expression[end] == '.'))
                {
                    end++;
                }
                var word = expression[position..end];
                position = end;
                var keywordToken = KeywordToken(word, dialect);
                if (keywordToken is not null)
                {
                    tokens.Add(keywordToken);
                    continue;
                }
                tokens.Add(new Token(TokenKind.Identifier, word));
                continue;
            }

            throw new ExpressionEvaluationError(expression, $"unexpected character '{character}' at position {position}.");
        }

        tokens.Add(new Token(TokenKind.End, string.Empty));
        return tokens;
    }

    static Token? KeywordToken(string word, ExpressionDialect dialect) => dialect switch
    {
        ExpressionDialect.MSBuild => word.ToLowerInvariant() switch
        {
            "and" => new Token(TokenKind.And, word),
            "or" => new Token(TokenKind.Or, word),
            "not" => new Token(TokenKind.Not, word),
            _ => null
        },
        ExpressionDialect.VB => word.ToLowerInvariant() switch
        {
            "andalso" or "and" => new Token(TokenKind.And, word),
            "orelse" or "or" => new Token(TokenKind.Or, word),
            "not" => new Token(TokenKind.Not, word),
            _ => null
        },
        _ => null
    };

    static char? Peek(string expression, int position) =>
        position < expression.Length ? expression[position] : null;
}
