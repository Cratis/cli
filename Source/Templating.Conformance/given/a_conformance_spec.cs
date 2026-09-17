// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Templating.Conformance.given;

/// <summary>
/// Base spec for rendering the vendored conformance templates through the real engine and asserting the expected output,
/// covering conditional processing across file families, value forms, generators, renames, copyOnly and
/// exclusions. The suite grows with the engine: every upstream construct adopted here gets a template first.
/// </summary>
public abstract class a_conformance_spec : Specification
{
    protected static string TemplatesRoot { get; } = FindTemplatesRoot();

    protected static InstantiationResult Instantiate(string templateName, string outputName, IReadOnlyDictionary<string, string>? parameters = null)
    {
        var templateRoot = Path.Combine(TemplatesRoot, templateName);
        var manifest = TemplateConfigParser.ParseFile(Path.Combine(templateRoot, ".template.config", "template.json"));
        var output = Path.Combine(Path.GetTempPath(), "cratis-conformance", Guid.NewGuid().ToString("N"));
        return new TemplateInstantiator().Instantiate(
            manifest,
            templateRoot,
            new InstantiationInputs(outputName, output, parameters ?? new Dictionary<string, string>()));
    }

    static string FindTemplatesRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !Directory.Exists(Path.Combine(current.FullName, "Templates")))
        {
            current = current.Parent;
        }
        return current is null
            ? throw new InvalidOperationException("Conformance templates not found next to the test assembly.")
            : Path.Combine(current.FullName, "Templates");
    }
}
