// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The kit installed into a project, recorded in <c language="csharp">.cratis/build/kit.json</c> by
/// <c language="csharp">cratis build init</c>: which kit, where it came from, which agent it was installed for, and every
/// project path the installation wrote. <c language="csharp">cratis build run</c> reads the kit from this record.
/// </summary>
/// <param name="Kit">The kit's name.</param>
/// <param name="Version">The kit's version.</param>
/// <param name="Sha256">The SHA-256 of a registered kit's archive, or <see langword="null"/>.</param>
/// <param name="Source">The Git URL or folder of a kit that is not registered, or <see langword="null"/>.</param>
/// <param name="Commit">The commit a Git URL resolved to, or <see langword="null"/>.</param>
/// <param name="Agent">The agent the kit was installed for, or <see langword="null"/>.</param>
/// <param name="Files">Every project path the installation wrote.</param>
public sealed record BuildProjectSettings(string Kit, string Version, string? Sha256, string? Source, string? Commit, string? Agent, IReadOnlyList<string> Files)
{
    /// <summary>The folder, relative to the project, holding the installed kit.</summary>
    public const string KitFolder = ".cratis/build/kit";

    /// <summary>The record, relative to the project.</summary>
    public const string File = ".cratis/build/kit.json";

    static readonly JsonSerializerOptions _indented = new() { WriteIndented = true };

    /// <summary>
    /// Reads the record of a project.
    /// </summary>
    /// <param name="project">The project folder.</param>
    /// <returns>The record, or <see langword="null"/> when the project has no kit.</returns>
    public static BuildProjectSettings? Read(string project)
    {
        var path = Path.Combine(project, File);
        if (!System.IO.File.Exists(path)) return null;
        var json = JsonNode.Parse(System.IO.File.ReadAllText(path));
        return new(
            json?["name"]?.GetValue<string>() ?? string.Empty,
            json?["version"]?.GetValue<string>() ?? string.Empty,
            json?["sha256"]?.GetValue<string>(),
            json?["source"]?["url"]?.GetValue<string>(),
            json?["source"]?["commit"]?.GetValue<string>(),
            json?["agent"]?.GetValue<string>(),
            [.. (json?["files"] as JsonArray ?? []).Select(file => file?.GetValue<string>()).OfType<string>()]);
    }

    /// <summary>
    /// Writes the record into a project.
    /// </summary>
    /// <param name="project">The project folder.</param>
    public void Write(string project)
    {
        var path = Path.Combine(project, File);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = new JsonObject { ["name"] = Kit, ["version"] = Version };
        if (Sha256 is not null) json["sha256"] = Sha256;
        if (Source is not null)
        {
            var source = new JsonObject { ["url"] = Source };
            if (Commit is not null) source["commit"] = Commit;
            json["source"] = source;
        }

        if (Agent is not null) json["agent"] = Agent;
        json["files"] = new JsonArray([.. Files.Select(file => (JsonNode)file)]);
        System.IO.File.WriteAllText(path, json.ToJsonString(_indented) + Environment.NewLine);
    }
}
