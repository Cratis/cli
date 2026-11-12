// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Enumeration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Cli.Extensions;

/// <summary>
/// Represents a publisher the project allows.
/// </summary>
/// <param name="Publisher">The identity of the publisher, as the lock records it.</param>
/// <param name="Key">The fingerprint of the publisher's key (<c language="csharp">sha256:&lt;hex&gt;</c>). Required for packages that carry their own key.</param>
/// <param name="References">The full references the publisher may supply, <c language="csharp">*</c> matches any run of characters; empty means any.</param>
public record AllowedPublisher(string Publisher, string? Key = null, IReadOnlyList<string>? References = null)
{
    /// <summary>
    /// Checks whether the publisher may supply a reference.
    /// </summary>
    /// <param name="resolved">The full reference.</param>
    /// <returns><see langword="true"/> when allowed.</returns>
    public bool Covers(string resolved) =>
        References is not { Count: > 0 } || References.Any(pattern => FileSystemName.MatchesSimpleExpression(pattern, resolved, false));
}

/// <summary>
/// Represents the project's allow-list of extension publishers, kept in <c language="csharp">.cratis/extensions.json</c>.
/// </summary>
/// <param name="publishers">The allowed publishers.</param>
public class ExtensionAllowList(IEnumerable<AllowedPublisher> publishers)
{
    /// <summary>The file beneath <c language="csharp">.cratis</c>.</summary>
    public const string FileName = "extensions.json";

    static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web) { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    /// <summary>
    /// Gets the allowed publishers.
    /// </summary>
    public IReadOnlyList<AllowedPublisher> Publishers { get; private set; } = [.. publishers];

    /// <summary>
    /// Gets the path of the allow-list for a project root.
    /// </summary>
    /// <param name="projectRoot">The project root holding <c language="csharp">.cratis</c>.</param>
    /// <returns>The path.</returns>
    public static string PathFor(string projectRoot) => Path.Combine(projectRoot, ".cratis", FileName);

    /// <summary>
    /// Loads the allow-list; a project without one allows nobody.
    /// </summary>
    /// <param name="projectRoot">The project root holding <c language="csharp">.cratis</c>.</param>
    /// <returns>The allow-list.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file is not valid.</exception>
    public static ExtensionAllowList Load(string projectRoot)
    {
        var path = PathFor(projectRoot);
        if (!File.Exists(path)) return new([]);
        try
        {
            var document = JsonSerializer.Deserialize<Document>(File.ReadAllText(path), _options);
            return new(document?.Publishers ?? []);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"{path} is not a valid extension allow-list: {exception.Message}", exception);
        }
    }

    /// <summary>
    /// Finds the entry for a publisher.
    /// </summary>
    /// <param name="publisher">The identity.</param>
    /// <returns>The entry, or <see langword="null"/>.</returns>
    public AllowedPublisher? Find(string publisher) => Publishers.FirstOrDefault(entry => entry.Publisher == publisher);

    /// <summary>
    /// Adds a publisher and writes the file.
    /// </summary>
    /// <param name="projectRoot">The project root holding <c language="csharp">.cratis</c>.</param>
    /// <param name="entry">The publisher to allow.</param>
    public void Add(string projectRoot, AllowedPublisher entry)
    {
        Publishers = [.. Publishers.Where(existing => existing.Publisher != entry.Publisher), entry];
        var path = PathFor(projectRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new Document { Publishers = Publishers }, _options) + "\n");
    }

    sealed class Document
    {
        public IReadOnlyList<AllowedPublisher> Publishers { get; set; } = [];
    }
}
