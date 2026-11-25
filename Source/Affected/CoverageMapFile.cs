// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Cratis.Affected;

/// <summary>
/// Reads and writes coverage maps. The text is deterministic: sorted, no timestamps, one unit per line so diffs stay readable.
/// </summary>
public static class CoverageMapFile
{
    static readonly JsonSerializerOptions _compact = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// Gets the repository relative path of the map for a test project.
    /// </summary>
    /// <param name="project">The test project file path.</param>
    /// <returns>The map path.</returns>
    public static string PathFor(string project) => $"{AffectedConfiguration.MapsFolder}/{project}.json";

    /// <summary>
    /// Renders a map as text.
    /// </summary>
    /// <param name="map">The map.</param>
    /// <returns>The text, ending with a newline.</returns>
    public static string Serialize(CoverageMap map)
    {
        var files = string.Join(",\n", map.Files.Select(file => "    " + Json(file)));
        var units = string.Join(",\n", map.Units.Select(unit =>
            "    {\"name\":" + Json(unit.Name) + ",\"covers\":" + Json(unit.Covers) + ",\"declaredIn\":" + Json(unit.DeclaredIn) + "}"));
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{{\n  \"schemaVersion\": {map.SchemaVersion},\n  \"builtAt\": {Json(map.BuiltAt)},\n  \"project\": {Json(map.Project)},\n  \"granularity\": {Json(map.Granularity.ToString().ToLowerInvariant())},\n  \"files\": [{Block(files)}],\n  \"units\": [{Block(units)}]\n}}\n");
    }

    /// <summary>
    /// Parses a map.
    /// </summary>
    /// <param name="text">The text of a map file.</param>
    /// <returns>The map.</returns>
    /// <exception cref="AffectedException">The text is corrupted, incomplete or of an unknown schema version.</exception>
    public static CoverageMap Parse(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            var version = root.GetProperty("schemaVersion").GetInt32();
            if (version != CoverageMap.CurrentSchemaVersion)
            {
                throw new AffectedException($"Unknown coverage map schema version {version}.");
            }

            var files = root.GetProperty("files").EnumerateArray().Select(element => element.GetString() ?? throw new AffectedException("A file name is null.")).ToList();
            var units = root.GetProperty("units").EnumerateArray().Select(element => new CoverageUnit(
                element.GetProperty("name").GetString() ?? throw new AffectedException("A unit name is null."),
                [.. element.GetProperty("covers").EnumerateArray().Select(item => Index(item, files))],
                [.. element.GetProperty("declaredIn").EnumerateArray().Select(item => Index(item, files))])).ToList();
            var granularity = Enum.Parse<UnitGranularity>(root.GetProperty("granularity").GetString() ?? string.Empty, ignoreCase: true);
            return new CoverageMap(
                version,
                root.GetProperty("builtAt").GetString() ?? throw new AffectedException("builtAt is null."),
                root.GetProperty("project").GetString() ?? throw new AffectedException("project is null."),
                granularity,
                files,
                units);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or ArgumentException)
        {
            throw new AffectedException($"The coverage map is corrupted: {exception.Message}", exception);
        }
    }

    /// <summary>
    /// Reads the map of a test project from the working tree.
    /// </summary>
    /// <param name="repositoryRoot">The working tree root.</param>
    /// <param name="project">The test project file path.</param>
    /// <returns>The map, or null when there is none.</returns>
    public static CoverageMap? Read(string repositoryRoot, string project)
    {
        var path = System.IO.Path.Combine(repositoryRoot, PathFor(project));
        return File.Exists(path) ? Parse(File.ReadAllText(path)) : null;
    }

    /// <summary>
    /// Writes the map of a test project, leaving the file untouched when the content is unchanged.
    /// </summary>
    /// <param name="repositoryRoot">The working tree root.</param>
    /// <param name="map">The map.</param>
    /// <returns>True when the file changed.</returns>
    public static bool Write(string repositoryRoot, CoverageMap map)
    {
        var path = System.IO.Path.Combine(repositoryRoot, PathFor(map.Project));
        var text = Serialize(map);
        if (File.Exists(path) && File.ReadAllText(path) == text)
        {
            return false;
        }

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, new UTF8Encoding(false));
        return true;
    }

    static string Json<T>(T value) => JsonSerializer.Serialize(value, _compact);

    static string Block(string lines) => lines.Length == 0 ? string.Empty : "\n" + lines + "\n  ";

    static int Index(JsonElement item, List<string> files)
    {
        var index = item.GetInt32();
        return index >= 0 && index < files.Count ? index : throw new AffectedException($"File index {index} is outside the file table.");
    }
}
