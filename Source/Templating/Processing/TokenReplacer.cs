// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

namespace Cratis.Templating.Processing;

/// <summary>
/// Performs token replacement as a single left-to-right pass: at each position the longest matching token
/// wins and scanning resumes after the replacement, so replaced content can never itself be replaced.
/// </summary>
public sealed class TokenReplacer
{
    readonly Dictionary<char, List<string>> _tokensByFirstChar = [];
    readonly Dictionary<string, string> _replacements = new(StringComparer.Ordinal);
    string? _longestToken;

    /// <summary>
    /// Gets the number of registered tokens.
    /// </summary>
    public int Count => _replacements.Count;

    /// <summary>
    /// Registers a token and its replacement, overriding any earlier registration of the same
    /// token. Reserved for the documented sourceName ambiguity, where several forms of one name
    /// legitimately compete for the same token and the later form wins.
    /// </summary>
    /// <param name="token">The token to match literally.</param>
    /// <param name="replacement">The replacement text.</param>
    public void AddOverride(string token, string replacement)
    {
        if (token.Length == 0)
        {
            return;
        }

        _replacements[token] = replacement;
        var first = token[0];
        if (!_tokensByFirstChar.TryGetValue(first, out var tokens))
        {
            tokens = [];
            _tokensByFirstChar[first] = tokens;
        }
        if (!tokens.Contains(token))
        {
            tokens.Add(token);
        }
    }

    /// <summary>
    /// Registers a token and its replacement.
    /// </summary>
    /// <param name="token">The token to match literally.</param>
    /// <param name="replacement">The replacement text.</param>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    public void Add(string token, string replacement)
    {
        if (token.Length == 0)
        {
            return;
        }

        if (_replacements.TryGetValue(token, out var existing) && existing != replacement)
        {
            throw new InvalidTemplateManifest(
                $"token '{token}' is registered with two different replacements ('{existing}' and '{replacement}').");
        }
        _replacements[token] = replacement;

        var first = token[0];
        if (!_tokensByFirstChar.TryGetValue(first, out var tokens))
        {
            tokens = [];
            _tokensByFirstChar[first] = tokens;
        }
        if (!tokens.Contains(token))
        {
            tokens.Add(token);
        }
        _longestToken = _longestToken is null || token.Length > _longestToken.Length ? token : _longestToken;
    }

    /// <summary>
    /// Determines whether a token is registered whose match text appears in the content.
    /// </summary>
    /// <param name="content">The content to inspect.</param>
    /// <returns>True when any registered token occurs in the content.</returns>
    public bool ContainsAnyToken(string content) =>
        _replacements.Keys.Any(token => content.Contains(token, StringComparison.Ordinal));

    /// <summary>
    /// Applies all replacements to content.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <returns>The content with every token occurrence replaced.</returns>
    public string Replace(string content)
    {
        if (_replacements.Count == 0)
        {
            return content;
        }

        var builder = new System.Text.StringBuilder(content.Length);
        var index = 0;
        while (index < content.Length)
        {
            var match = MatchAt(content, index);
            if (match is null)
            {
                builder.Append(content[index]);
                index++;
                continue;
            }
            builder.Append(_replacements[match]);
            index += match.Length;
        }
        return builder.ToString();
    }

    /// <summary>
    /// Applies all replacements to a path, also normalizing directory separators.
    /// </summary>
    /// <param name="path">The relative path.</param>
    /// <returns>The path with tokens replaced.</returns>
    public string ReplacePath(string path) => Replace(path).Replace('\\', '/');

    string? MatchAt(string content, int index)
    {
        if (!_tokensByFirstChar.TryGetValue(content[index], out var tokens))
        {
            return null;
        }

        string? best = null;
        foreach (var token in tokens)
        {
            if ((best is null || token.Length > best.Length)
                && index + token.Length <= content.Length
                && content.AsSpan(index, token.Length).SequenceEqual(token))
            {
                best = token;
            }
        }
        return best;
    }
}
