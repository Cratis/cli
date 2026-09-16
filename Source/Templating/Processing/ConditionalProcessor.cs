// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

using System.Text.RegularExpressions;
using Cratis.Templating.Expressions;

namespace Cratis.Templating.Processing;

/// <summary>
/// Processes conditional directives in file content: <c language="csharp">#if</c>, <c language="csharp">#elseif</c>, <c language="csharp">#else</c>, <c language="csharp">#endif</c> with
/// their actionable variants, balanced nesting, the literal-<c language="csharp">FALSE</c> verbatim-exclusion form, the
/// <c language="csharp">cnd:noEmit</c> on/off markers for partial-file exclusion, and MSBuild <c language="csharp">Condition</c> attributes.
/// </summary>
public static partial class ConditionalProcessor
{
    /// <summary>
    /// Directive kind recognized on a line.
    /// </summary>
    enum DirectiveKind
    {
        /// <summary>Not a directive.</summary>
        None,

        /// <summary>Opens a conditional block.</summary>
        If,

        /// <summary>Alternate branch.</summary>
        Elseif,

        /// <summary>Fallback branch.</summary>
        Else,

        /// <summary>Closes the block.</summary>
        Endif
    }

    [GeneratedRegex(@"\s+Condition=(?<condition>""[^""]*""|'[^']*'|[^ >/]+)", RegexOptions.None, 2000)]
    private static partial Regex ConditionAttributeRegex { get; }

    [GeneratedRegex(@"\$\((?<name>[^)]+)\)", RegexOptions.None, 2000)]
    private static partial Regex VariableRegex { get; }

    /// <summary>
    /// Processes conditional directives in content.
    /// </summary>
    /// <param name="content">The file content.</param>
    /// <param name="configuration">The directive configuration for the file.</param>
    /// <param name="dialect">The expression dialect.</param>
    /// <param name="scope">Symbol values keyed by name.</param>
    /// <param name="filePath">File path, for error messages.</param>
    /// <param name="knownLiterals">Opted-in quoteless choice literals, or null for none.</param>
    /// <returns>The processed content.</returns>
    public static string Process(
        string content,
        FileFamilyConfig configuration,
        ExpressionDialect dialect,
        IReadOnlyDictionary<string, string> scope,
        string filePath,
        IReadOnlyCollection<string>? knownLiterals = null) =>
        new Engine(configuration, dialect, scope, filePath, knownLiterals ?? []).Run(content);

    /// <summary>
    /// Determines whether content contains any conditional directive for the given configuration.
    /// </summary>
    /// <param name="content">The file content.</param>
    /// <param name="configuration">The directive configuration.</param>
    /// <returns>True when a directive is present.</returns>
    public static bool ContainsDirectives(string content, FileFamilyConfig configuration) =>
        Tokens(configuration).Any(token => content.Contains(token, StringComparison.Ordinal));

    /// <summary>
    /// Processes MSBuild Condition attributes on elements: a satisfied condition drops the attribute; a
    /// false condition drops the whole element. Disabled by the documented noEmit marker.
    /// </summary>
    /// <param name="content">The file content.</param>
    /// <param name="scope">Symbol values keyed by name.</param>
    /// <param name="filePath">File path, for error messages.</param>
    /// <returns>The processed content.</returns>
    public static string ProcessMsBuildConditions(string content, IReadOnlyDictionary<string, string> scope, string filePath)
    {
        if (content.Contains("msbuild-conditional:noEmit", StringComparison.Ordinal)
            || !ConditionAttributeRegex.IsMatch(content))
        {
            return content;
        }

        var result = content;
        foreach (Match match in ConditionAttributeRegex.Matches(content))
        {
            var condition = StripAttributeQuotes(match.Groups["condition"].Value);
            var expanded = ExpandVariables(condition, scope);
            bool satisfied;
            try
            {
                satisfied = ExpressionEvaluator.EvaluateBoolean(expanded, ExpressionDialect.MSBuild, scope);
            }
            catch (ExpressionEvaluationError)
            {
                // Unresolvable references make the condition unevaluatable; the attribute is preserved.
                continue;
            }

            if (!satisfied)
            {
                result = RemoveElement(result, match.Index);
            }
            else
            {
                result = result.Remove(match.Index, match.Length);
            }

            // Re-run on the mutated content so offsets stay valid.
            return ProcessMsBuildConditions(result, scope, filePath);
        }
        return result;
    }

    static IEnumerable<string> Tokens(FileFamilyConfig configuration) =>
        configuration.IfTokens.Concat(configuration.ElseifTokens).Concat(configuration.ElseTokens)
            .Concat(configuration.EndifTokens).Concat(configuration.ActionableIfTokens)
            .Concat(configuration.ActionableElseifTokens).Concat(configuration.ActionableElseTokens);

    static string StripAttributeQuotes(string value)
    {
        // Strip exactly one paired quote — the attribute's delimiters — so that empty-string
        // literals at the end of a condition (e.g. == '') survive.
        return value.Length >= 2 && value[0] == value[^1] && value[0] is '"' or '\''
            ? value[1..^1]
            : value;
    }

    static string ExpandVariables(string condition, IReadOnlyDictionary<string, string> scope)
    {
        // MSBuild property expansion happens before evaluation and does not add quotes: the source
        // text supplies them ('$(X)'), and a bare $(X) expands to the raw value — empty when undefined.
        return VariableRegex.Replace(condition, match =>
            scope.TryGetValue(match.Groups["name"].Value, out var value) ? value : string.Empty);
    }

    static string RemoveElement(string content, int attributeIndex)
    {
        var elementStart = content.LastIndexOf('<', attributeIndex);
        if (elementStart < 0)
        {
            return content;
        }

        var tagName = content[(elementStart + 1)..].Split(['>', ' ', '\t', '\r', '\n'], 2)[0];
        var selfClosingEnd = FindSelfClosingEnd(content, elementStart);
        if (selfClosingEnd >= 0)
        {
            return content.Remove(elementStart, selfClosingEnd - elementStart + 1);
        }

        var closingTag = $"</{tagName}>";
        var elementEnd = content.IndexOf(closingTag, elementStart, StringComparison.Ordinal);
        if (elementEnd < 0)
        {
            // No closing tag found — remove to end of the opening tag as a conservative fallback.
            var openEnd = content.IndexOf('>', elementStart);
            return openEnd >= 0 ? content.Remove(elementStart, openEnd - elementStart + 1) : content;
        }

        var end = elementEnd + closingTag.Length;

        // Swallow the line when nothing but whitespace precedes the element and follows its end.
        var lineStart = content.LastIndexOf('\n', elementStart);
        var lineEnd = content.IndexOf('\n', end);
        var onlyWhitespaceAround =
            content[(lineStart + 1)..elementStart].Trim().Length == 0 &&
            (lineEnd < 0 || content[end..lineEnd].Trim().Length == 0);
        return onlyWhitespaceAround
            ? content.Remove(lineStart + 1, (lineEnd < 0 ? content.Length : lineEnd + 1) - lineStart - 1)
            : content.Remove(elementStart, end - elementStart);
    }

    static int FindSelfClosingEnd(string content, int elementStart)
    {
        var openEnd = content.IndexOf('>', elementStart);
        return openEnd > 0 && content[openEnd - 1] == '/' ? openEnd : -1;
    }

    sealed class Engine(
        FileFamilyConfig configuration,
        ExpressionDialect dialect,
        IReadOnlyDictionary<string, string> scope,
        string filePath,
        IReadOnlyCollection<string> knownLiterals)
    {
        readonly List<Block> _blocks = [];

        public string Run(string content)
        {
            var lines = content.Replace("\r\n", "\n").Split('\n');
            var output = new List<string>(lines.Length);
            var noEmit = false;

            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                var trimmed = line.TrimStart();

                if (noEmit)
                {
                    if (IsOnOffMarker(trimmed, "+"))
                    {
                        noEmit = false;
                        continue;
                    }
                    output.Add(line);
                    continue;
                }

                if (IsOnOffMarker(trimmed, "-"))
                {
                    noEmit = true;
                    continue;
                }

                var (kind, expression, actionable) = MatchDirective(trimmed);
                switch (kind)
                {
                    case DirectiveKind.If:
                        Open(expression, actionable, index);
                        continue;

                    case DirectiveKind.Elseif:
                        Current(index).EnterElseif(expression, actionable, this);
                        continue;

                    case DirectiveKind.Else:
                        Current(index).EnterElse(actionable);
                        continue;

                    case DirectiveKind.Endif:
                        Current(index);
                        _blocks.RemoveAt(_blocks.Count - 1);
                        continue;
                }

                if (_blocks.Count == 0 || _blocks.TrueForAll(block => block.Emitting))
                {
                    output.Add(ApplyUncomment(line));
                }
            }

            if (_blocks.Count > 0)
            {
                throw new ExpressionEvaluationError(
                    string.Empty,
                    $"{filePath}: missing #endif for #if at line {_blocks[0].LineNumber + 1}.");
            }

            return string.Join('\n', output);
        }

        static bool MatchesToken(string trimmed, IReadOnlyList<string> tokens) =>
            tokens.Any(token => TokenFollowedByBoundary(trimmed, token));

        static string? Match(string trimmed, IReadOnlyList<string> tokens)
        {
            foreach (var token in tokens.OrderByDescending(t => t.Length))
            {
                if (TokenFollowedByBoundary(trimmed, token))
                {
                    return ExtractExpression(trimmed[token.Length..]);
                }
            }
            return null;
        }

        static bool TokenFollowedByBoundary(string trimmed, string token) =>
            trimmed.StartsWith(token, StringComparison.Ordinal)
            && (trimmed.Length == token.Length
                || char.IsWhiteSpace(trimmed[token.Length])
                || trimmed[token.Length] == '(');

        static string ExtractExpression(string rest)
        {
            rest = rest.Trim();
            foreach (var suffix in new[] { "-->", "*/", "*@" })
            {
                if (rest.EndsWith(suffix, StringComparison.Ordinal))
                {
                    rest = rest[..^suffix.Length].Trim();
                }
            }

            if (!rest.StartsWith('('))
            {
                return rest;
            }

            var depth = 0;
            for (var index = 0; index < rest.Length; index++)
            {
                if (rest[index] == '(')
                {
                    depth++;
                }
                else if (rest[index] == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return rest[1..index];
                    }
                }
            }
            return rest[1..];
        }

        void Open(string expression, bool actionable, int lineNumber)
        {
            // Inside a non-emitting or verbatim block, nested directives are counted for balance only —
            // their expressions are never evaluated.
            var enclosing = _blocks.TrueForAll(block => block.Emitting && !block.Verbatim);
            if (!enclosing)
            {
                _blocks.Add(Block.Inert(lineNumber));
                return;
            }

            if (expression.Trim().Equals("FALSE", StringComparison.OrdinalIgnoreCase))
            {
                _blocks.Add(Block.VerbatimFalse(lineNumber));
                return;
            }

            _blocks.Add(Block.Active(lineNumber, actionable, Evaluate(expression)));
        }

        bool Evaluate(string expression) =>
            ExpressionEvaluator.EvaluateBoolean(expression, dialect, scope, knownLiterals);

        Block Current(int lineNumber)
        {
            if (_blocks.Count == 0)
            {
                throw new ExpressionEvaluationError(
                    string.Empty,
                    $"{filePath}: conditional directive at line {lineNumber + 1} has no matching #if.");
            }
            return _blocks[^1];
        }

        string ApplyUncomment(string line)
        {
            var block = _blocks.LastOrDefault(block => block.ActionableBranch);
            if (block is null)
            {
                return line;
            }

            foreach (var action in configuration.UncommentActions)
            {
                line = action switch
                {
                    "cStyleUncomment" => line.TrimStart().StartsWith("//", StringComparison.Ordinal)
                        && !line.TrimStart().StartsWith("////", StringComparison.Ordinal)
                            ? line.Remove(line.IndexOf("//", StringComparison.Ordinal), 2)
                            : line,
                    "cStyleReduceComment" => line.TrimStart().StartsWith("////", StringComparison.Ordinal)
                        ? line.Remove(line.IndexOf("////", StringComparison.Ordinal), 4)
                              .Insert(line.IndexOf("////", StringComparison.Ordinal), "//")
                        : line,
                    "xmlUncomment" => line.Replace("<!--", string.Empty).Replace("-->", string.Empty),
                    _ => throw new UnsupportedTemplateConstruct(
                        $"actions: '{action}'",
                        $"uncomment action '{action}' is not implemented. Known actions: cStyleUncomment, cStyleReduceComment, xmlUncomment.")
                };
            }
            return line;
        }

        bool IsOnOffMarker(string trimmed, string sign)
        {
            if (!trimmed.StartsWith(configuration.OnOffPrefix, StringComparison.Ordinal))
            {
                return false;
            }
            var rest = trimmed[configuration.OnOffPrefix.Length..].TrimStart();
            return rest.StartsWith($"{sign}:cnd:noEmit", StringComparison.Ordinal);
        }

        (DirectiveKind Kind, string Expression, bool Actionable) MatchDirective(string trimmed)
        {
            var expression = Match(trimmed, configuration.ActionableIfTokens);
            if (expression is not null)
            {
                return (DirectiveKind.If, expression, true);
            }
            expression = Match(trimmed, configuration.IfTokens);
            if (expression is not null)
            {
                return (DirectiveKind.If, expression, false);
            }
            expression = Match(trimmed, configuration.ActionableElseifTokens);
            if (expression is not null)
            {
                return (DirectiveKind.Elseif, expression, true);
            }
            expression = Match(trimmed, configuration.ElseifTokens);
            if (expression is not null)
            {
                return (DirectiveKind.Elseif, expression, false);
            }
            if (MatchesToken(trimmed, configuration.ActionableElseTokens))
            {
                return (DirectiveKind.Else, string.Empty, true);
            }
            if (MatchesToken(trimmed, configuration.ElseTokens))
            {
                return (DirectiveKind.Else, string.Empty, false);
            }
            return MatchesToken(trimmed, configuration.EndifTokens)
                ? (DirectiveKind.Endif, string.Empty, false)
                : (DirectiveKind.None, string.Empty, false);
        }

        sealed class Block
        {
            Block(bool emitting, bool inert, bool verbatim, int lineNumber)
            {
                Emitting = emitting;
                IsInert = inert;
                Verbatim = verbatim;
                LineNumber = lineNumber;
            }

            public bool Emitting { get; private set; }

            public bool IsInert { get; }

            public bool Verbatim { get; }

            public int LineNumber { get; }

            public bool BranchTaken { get; private set; }

            /// <summary>
            /// Gets a value indicating whether the currently active branch was opened with an actionable
            /// directive and should have its body uncommented.
            /// </summary>
            public bool ActionableBranch { get; private set; }

            public static Block Active(int lineNumber, bool actionable, bool emitting) => new(emitting, false, false, lineNumber)
            {
                ActionableBranch = actionable && emitting,
                BranchTaken = emitting
            };

            public static Block Inert(int lineNumber) => new(false, true, false, lineNumber);

            public static Block VerbatimFalse(int lineNumber) => new(false, false, true, lineNumber);

            public void EnterElseif(string expression, bool actionable, Engine engine)
            {
                if (IsInert || Verbatim || BranchTaken)
                {
                    Emitting = false;
                    ActionableBranch = false;
                    return;
                }

                Emitting = engine.Evaluate(expression);
                ActionableBranch = Emitting && actionable;
                BranchTaken |= Emitting;
            }

            public void EnterElse(bool actionable)
            {
                if (IsInert || Verbatim)
                {
                    Emitting = false;
                    ActionableBranch = false;
                    return;
                }

                Emitting = !BranchTaken;
                ActionableBranch = Emitting && actionable;
            }
        }
    }
}
