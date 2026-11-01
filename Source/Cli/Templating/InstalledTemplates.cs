// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Templates;

/// <summary>
/// The template set installed under <c language="bash">~/.cratis/templates</c>: the version <c language="bash">cratis new</c>
/// uses and the packages that make it up. Recorded in <c language="bash">installed.json</c>, which is only ever
/// replaced as a whole, so a failed download never leaves a half-switched installation.
/// </summary>
/// <param name="Version">The installed template version.</param>
/// <param name="Packages">The packages of the set, all at <paramref name="Version"/>.</param>
/// <param name="MinimumCliVersion">The minimum CLI version the set declares, if any.</param>
/// <param name="InstalledAt">When the set was installed.</param>
public record InstalledTemplates(string Version, IReadOnlyList<string> Packages, string? MinimumCliVersion, DateTimeOffset InstalledAt)
{
    /// <summary>
    /// The name of the file recording the installed set.
    /// </summary>
    public const string FileName = "installed.json";

    /// <summary>
    /// Reads the installed set from a templates root.
    /// </summary>
    /// <param name="templatesRoot">The templates root, normally <c language="bash">~/.cratis/templates</c>.</param>
    /// <returns>The installed set, or null when none is recorded or the record cannot be read.</returns>
    public static InstalledTemplates? Read(string templatesRoot)
    {
        var path = Path.Combine(templatesRoot, FileName);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var document = JsonNode.Parse(File.ReadAllText(path))?.AsObject();
            var version = document?["version"]?.GetValue<string>();
            var packages = document?["packages"]?.AsArray().OfType<JsonNode>().Select(package => package.GetValue<string>()).ToArray();
            if (version is null || packages is null || packages.Length == 0)
            {
                return null;
            }

            var installedAt = document!["installedAt"]?.GetValue<DateTimeOffset>() ?? DateTimeOffset.MinValue;
            return new(version, packages, document["minimumCliVersion"]?.GetValue<string>(), installedAt);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Writes the set as the installed one. The record is written to a temporary file first and then
    /// moved over the previous record, so readers see either the old or the new set — never a partial one.
    /// </summary>
    /// <param name="templatesRoot">The templates root.</param>
    public void Write(string templatesRoot)
    {
        Directory.CreateDirectory(templatesRoot);
        var document = new JsonObject
        {
            ["version"] = Version,
            ["packages"] = JsonSerializer.SerializeToNode(Packages),
            ["minimumCliVersion"] = MinimumCliVersion,
            ["installedAt"] = InstalledAt
        };
        var path = Path.Combine(templatesRoot, FileName);
        var temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(temporary, document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, path, overwrite: true);
    }
}
