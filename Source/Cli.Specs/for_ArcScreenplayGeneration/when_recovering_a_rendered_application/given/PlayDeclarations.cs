// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Cli.for_ArcScreenplayGeneration.when_recovering_a_rendered_application.given;

/// <summary>
/// Reads the contract declarations of a Screenplay document: every command, event, read model and query, the members of
/// the first three, and the result and parameters of a query.
/// </summary>
/// <remarks>
/// Directive lines - <c language="csharp">file</c>, <c language="csharp">description</c>, <c language="csharp">produces</c>
/// and the like - describe where or how a declaration is realized and are not members. The query result is read
/// without its <c language="csharp">observable</c> modifier: whether a query streams is reported separately, because it
/// is a property of the rendered code rather than of the contract.
/// </remarks>
internal static partial class PlayDeclarations
{
    static readonly HashSet<string> _directives = new(StringComparer.Ordinal)
    {
        "file", "description", "produces", "for", "validate", "authorize", "handler", "reads", "concurrency",
        "returns", "observable", "performer", "filter", "scoped", "by"
    };

    /// <summary>
    /// Reads the declarations of a document.
    /// </summary>
    /// <param name="text">The document text.</param>
    /// <returns>The declarations.</returns>
    internal static IReadOnlySet<string> In(string text)
    {
        var declarations = new HashSet<string>(StringComparer.Ordinal);
        string? current = null;
        var indentation = 0;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var depth = raw.Length - raw.TrimStart().Length;
            if (DeclarationPattern.Match(line) is { Success: true } declaration)
            {
                current = $"{declaration.Groups["kind"].Value} {declaration.Groups["name"].Value}";
                indentation = depth;
                declarations.Add(current);
                if (QueryResultPattern.Match(line) is { Success: true } result)
                {
                    declarations.Add($"{current} => {result.Groups["type"].Value}");
                }

                continue;
            }

            if (current is null || depth <= indentation)
            {
                current = null;
                continue;
            }

            if (depth != indentation + 2)
            {
                continue;
            }

            var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (current.StartsWith("query ", StringComparison.Ordinal))
            {
                if (words is ["by", var parameter, ..])
                {
                    declarations.Add($"{current} by {parameter}");
                }
            }
            else if (words.Length >= 2 && !_directives.Contains(words[0]) && char.IsLower(words[0][0]))
            {
                declarations.Add($"{current}.{words[0]}");
            }
        }

        return declarations;
    }

    [GeneratedRegex("^(?<kind>command|event|readmodel|query) (?<name>[A-Za-z]+)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex DeclarationPattern { get; }

    [GeneratedRegex("=>\\s*(?:observable\\s+)?(?<type>[A-Za-z]+)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex QueryResultPattern { get; }
}
