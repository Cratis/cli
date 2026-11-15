// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Scaffolding;

namespace Cratis.Cli.Extensions;

/// <summary>
/// Writes a small pair of templates that follow <see cref="ExtensionTemplateInterface"/>, standing in for Cratis.Templates.Extensions.
/// </summary>
static class ExtensionTemplateFixture
{
    /// <summary>
    /// Creates a folder holding the extension and the package template.
    /// </summary>
    /// <returns>The folder.</returns>
    public static string Create()
    {
        var root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-extension-new", Guid.NewGuid().ToString("N"))).FullName;
        var extensionSymbols = Symbols(
            Choice("Backend", "BACKENDKIND", "none", "csharp", "kotlin", "go", "typescript", "none"),
            Choice("Frontend", "FRONTENDKIND", "none", "react", "mount", "screenplay", "none"));
        Write(root, "extension", ExtensionTemplateInterface.ExtensionShortName, "Cratis.Sample", extensionSymbols, ExtensionContract, "README.md");
        var packageSymbols = Symbols(Choice("Component", "COMPONENTKIND", "react", "react", "mount"));
        Write(root, "package", ExtensionTemplateInterface.PackageShortName, "Cratis.Sample", packageSymbols, PackageContract, "README.md");
        return root;
    }

    const string Head = "extension Cratis.Sample\n  display name \"DISPLAYNAME\"\n  description \"DESCRIPTIONTEXT\"\n  icon \"assets/icon.svg\"\n  image \"assets/image.svg\"\n    caption \"The package in use\"\n  requires stage \"^5\"\n  requires platform web\n";
    const string ExtensionContract = Head + "\n  ability Ping idempotent\n    text String\n";
    const string PackageContract = Head + "\n  component Sample\n    kind COMPONENTKIND\n    platform web\n    placeholder \"placeholders/sample.html\"\n    property title String\n\n  frontend \"frontend/index.js\"\n";

    static string Symbols(params string[] choices) =>
        string.Join(",\n", [
            .. choices,
            Text("DisplayName", "DISPLAYNAME"),
            Text("Description", "DESCRIPTIONTEXT"),
            Text("Publisher", "PUBLISHERNAME")]);

    static string Text(string name, string replaces) =>
        $"\"{name}\": {{ \"type\": \"parameter\", \"datatype\": \"text\", \"defaultValue\": \"x\", \"replaces\": \"{replaces}\" }}";

    static string Choice(string name, string replaces, string defaultValue, params string[] choices) =>
        $"\"{name}\": {{ \"type\": \"parameter\", \"datatype\": \"choice\", \"defaultValue\": \"{defaultValue}\", \"replaces\": \"{replaces}\", \"choices\": [{string.Join(", ", choices.Select(choice => $"{{ \"choice\": \"{choice}\" }}"))}] }}";

    static void Write(string root, string folder, string shortName, string sourceName, string symbols, string contract, string readme)
    {
        var directory = Directory.CreateDirectory(Path.Combine(root, folder));
        Directory.CreateDirectory(Path.Combine(directory.FullName, ".template.config"));
        var manifest = string.Join('\n', [
            "{",
            $"  \"identity\": \"Spec.{shortName}\",",
            $"  \"name\": \"{shortName}\",",
            $"  \"shortName\": \"{shortName}\",",
            $"  \"sourceName\": \"{sourceName}\",",
            $"  \"symbols\": {{ {symbols} }}",
            "}"]);
        File.WriteAllText(Path.Combine(directory.FullName, ".template.config", "template.json"), manifest);
        File.WriteAllText(Path.Combine(directory.FullName, "contract.play"), contract);
        File.WriteAllText(Path.Combine(directory.FullName, "README.md"), "# Cratis.Sample\nBackend: BACKENDKIND\nFrontend: FRONTENDKIND\nComponent: COMPONENTKIND\nPublisher: PUBLISHERNAME\n");
        Directory.CreateDirectory(Path.Combine(directory.FullName, "assets"));
        File.WriteAllText(Path.Combine(directory.FullName, "assets", "icon.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");
        File.WriteAllText(Path.Combine(directory.FullName, "assets", "image.svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");
        if (contract.Contains("component", StringComparison.Ordinal))
        {
            Directory.CreateDirectory(Path.Combine(directory.FullName, "placeholders"));
            File.WriteAllText(Path.Combine(directory.FullName, "placeholders", "sample.html"), "<div style=\"padding:8px\">Sample</div>");
            Directory.CreateDirectory(Path.Combine(directory.FullName, "frontend"));
            File.WriteAllText(Path.Combine(directory.FullName, "frontend", "index.js"), "export {};");
        }
    }
}
