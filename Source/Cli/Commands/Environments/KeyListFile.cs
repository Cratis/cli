// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Reads the keys a model needs from a key list: a JSON array of strings, a JSON object with a <c language="csharp">keys</c> array, or a text file with one key per line
/// (blank lines and lines starting with <c language="csharp">#</c> are ignored).
/// </summary>
public class KeyListFile : IModelKeySource
{
    /// <inheritdoc/>
    public bool CanRead(string path) =>
        File.Exists(path) && !string.Equals(Path.GetExtension(path), ".play", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public IReadOnlyList<string> ReadKeys(string path)
    {
        var text = File.ReadAllText(path);
        if (string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
        {
            using var json = JsonDocument.Parse(text);
            var array = json.RootElement.ValueKind == JsonValueKind.Object && json.RootElement.TryGetProperty("keys", out var keys) ? keys : json.RootElement;
            return array.ValueKind == JsonValueKind.Array
                ? [.. array.EnumerateArray().Select(element => element.GetString()).OfType<string>().Distinct(StringComparer.Ordinal)]
                : throw new InvalidOperationException($"{path} must be a JSON array of keys or an object with a 'keys' array.");
        }

        return [.. text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith('#')).Distinct(StringComparer.Ordinal)];
    }
}
