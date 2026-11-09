// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// A build kit unpacked on disk: the manifest and the files it names.
/// </summary>
/// <param name="Root">The kit's root folder.</param>
/// <param name="Manifest">The manifest, as JSON.</param>
public sealed record BuildKit(string Root, JsonObject Manifest)
{
    /// <summary>Gets the kit's name.</summary>
    public string Name => Text("name") ?? string.Empty;

    /// <summary>Gets the kit's own version.</summary>
    public string Version => Text("version") ?? string.Empty;

    /// <summary>Gets the one-sentence description of the stack.</summary>
    public string Description => Text("description") ?? string.Empty;

    /// <summary>Gets the slice types the kit builds.</summary>
    public IReadOnlyList<string> SliceTypes => Texts(Manifest["sliceTypes"]);

    /// <summary>Gets a value indicating whether the kit also builds user interfaces.</summary>
    public bool BuildsUserInterfaces => Manifest["ui"] is JsonValue value && value.TryGetValue<bool>(out var ui) && ui;

    /// <summary>Gets the contract versions the kit's prompt is written for.</summary>
    public IReadOnlyList<string> ContractVersions => Texts(Manifest["contract"]?["versions"]) is { Count: > 0 } versions ? versions : ["1"];

    /// <summary>Gets the kit folder copied into a new project, or <see langword="null"/>.</summary>
    public string? ScaffoldPath => Manifest["scaffold"] is JsonValue ? Text("scaffold") : Text(Manifest["scaffold"]?["path"]);

    /// <summary>Gets the commands run in a new project after the scaffold is copied.</summary>
    public IReadOnlyList<string> ScaffoldCommands => Texts(Manifest["scaffold"] is JsonObject scaffold ? scaffold["commands"] : null);

    /// <summary>Gets the path of the stack instructions.</summary>
    public string Instructions => Text("instructions") ?? string.Empty;

    /// <summary>Gets the path of the skills folder, or <see langword="null"/>.</summary>
    public string? Skills => Text("skills");

    /// <summary>Gets the path of the prompt template.</summary>
    public string Prompt => Text("prompt") ?? string.Empty;

    /// <summary>Gets the verification commands.</summary>
    public IReadOnlyList<string> Verify => Texts(Manifest["verify"]);

    /// <summary>Gets the template of the branch to build on in a builder-managed repository.</summary>
    public string? GitBranch => Text(Manifest["git"]?["branch"]);

    /// <summary>Gets the template of the commit message in a builder-managed repository.</summary>
    public string? GitCommit => Text(Manifest["git"]?["commit"]);

    /// <summary>Gets the globs, relative to the project, the agent may write.</summary>
    public IReadOnlyList<string> PermittedPaths => Manifest["permissions"]?["paths"] is JsonArray ? Texts(Manifest["permissions"]?["paths"]) : ["**"];

    /// <summary>Gets the command prefixes the agent may run, besides the verification commands.</summary>
    public IReadOnlyList<string> PermittedCommands => Texts(Manifest["permissions"]?["commands"]);

    /// <summary>Gets the path of the prompt that learns an existing codebase, or <see langword="null"/>.</summary>
    public string? Learn => Text(Manifest["existingCodebase"]?["learn"]);

    /// <summary>Gets the project path the learned conventions are recorded at, or <see langword="null"/>.</summary>
    public string? Conventions => Text(Manifest["existingCodebase"]?["conventions"]);

    /// <summary>Gets the commands the host checks for, each with an optional minimum version.</summary>
    public IReadOnlyList<(string Command, string? Version)> Requires =>
        [.. (Manifest["requires"] as JsonArray ?? []).OfType<JsonObject>().Select(entry => (Text(entry["command"]) ?? string.Empty, Text(entry["version"])))];

    /// <summary>
    /// Loads the kit at a folder and validates it.
    /// </summary>
    /// <param name="root">The kit's root folder.</param>
    /// <returns>The kit.</returns>
    /// <exception cref="InvalidBuildKit">The kit has no manifest, or the manifest has problems.</exception>
    public static BuildKit Load(string root)
    {
        var full = Path.GetFullPath(root);
        var manifestPath = BuildKitManifestText.Find(full) ??
            throw new InvalidBuildKit([$"(document): there is no {BuildKitManifestText.YamlFileName} or {BuildKitManifestText.JsonFileName} at the root of {full}."]);
        var manifest = BuildKitManifestText.Read(File.ReadAllText(manifestPath), manifestPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
        var kit = new BuildKit(full, manifest);
        var problems = BuildKitValidation.Problems(kit).ToList();
        return problems.Count == 0 ? kit : throw new InvalidBuildKit(problems);
    }

    /// <summary>
    /// Resolves a path inside the kit.
    /// </summary>
    /// <param name="kitPath">The path, starting with <c language="csharp">./</c>.</param>
    /// <returns>The full path.</returns>
    public string PathOf(string kitPath) => Path.GetFullPath(Path.Combine(Root, kitPath));

    /// <summary>
    /// Reads a file of the kit.
    /// </summary>
    /// <param name="kitPath">The path, starting with <c language="csharp">./</c>.</param>
    /// <returns>The file's text.</returns>
    public string ReadText(string kitPath) => File.ReadAllText(PathOf(kitPath));

    string? Text(string member) => Text(Manifest[member]);

    static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    static List<string> Texts(JsonNode? node) => [.. (node as JsonArray ?? []).Select(Text).OfType<string>()];
}
