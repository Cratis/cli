// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using System.Xml.Linq;

namespace Cratis.Affected;

/// <summary>
/// Reads the facts the computation needs out of the text of a project file, without evaluating MSBuild.
/// Anything it cannot resolve statically is left out, and the callers compensate by being conservative.
/// </summary>
public static class ProjectParser
{
    static readonly string[] _itemTypes = ["Compile", "Content", "None", "EmbeddedResource", "AdditionalFiles", "Protobuf", "Watch"];

    /// <summary>
    /// Parses one project file.
    /// </summary>
    /// <param name="path">The repository relative path of the project file.</param>
    /// <param name="xml">The text of the project file.</param>
    /// <param name="configuration">The repository configuration.</param>
    /// <returns>The project; reference paths are not yet checked against the projects that exist.</returns>
    /// <exception cref="AffectedException">The text is not valid XML.</exception>
    public static ProjectDefinition Parse(string path, string xml, AffectedConfiguration configuration)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(xml);
        }
        catch (XmlException exception)
        {
            throw new AffectedException($"{path} is not valid project XML: {exception.Message}", exception);
        }

        var directory = PathNormalizer.DirectoryOf(path);
        var references = new List<string>();
        var analyzers = new List<string>();
        var externals = new List<string>();
        var isTest = Glob.IsMatchAny(configuration.TestProjects, path);
        bool? declaredTest = null;
        var isGenerator = Glob.IsMatchAny(configuration.GeneratorProjects, path);

        foreach (var element in document.Descendants())
        {
            var name = element.Name.LocalName;
            switch (name)
            {
                case "ProjectReference":
                    foreach (var reference in Values(element, "Include", "Update"))
                    {
                        var resolved = PathNormalizer.Resolve(directory, reference);
                        if (resolved is null)
                        {
                            continue;
                        }

                        references.Add(resolved);
                        if (string.Equals((string?)element.Attribute("OutputItemType"), "Analyzer", StringComparison.OrdinalIgnoreCase))
                        {
                            analyzers.Add(resolved);
                        }
                    }

                    break;

                case "PackageReference":
                    var package = (string?)element.Attribute("Include") ?? (string?)element.Attribute("Update") ?? string.Empty;
                    isTest |= package.Equals("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase)
                        || package.StartsWith("xunit.v3", StringComparison.OrdinalIgnoreCase);
                    break;

                case "IsTestProject":
                    // The last declaration wins, as in MSBuild; an explicit false is stronger than a test package.
                    declaredTest = IsTrue(element);
                    break;

                case "IsRoslynComponent":
                case "EnforceExtendedAnalyzerRules":
                    isGenerator |= IsTrue(element);
                    break;

                case "Import":
                    AddExternal(directory, (string?)element.Attribute("Project"), externals);
                    break;

                default:
                    if (_itemTypes.Contains(name))
                    {
                        foreach (var item in Values(element, "Include", "Update"))
                        {
                            AddExternal(directory, item, externals);
                        }
                    }

                    break;
            }
        }

        if (declaredTest is not null)
        {
            isTest = declaredTest.Value || Glob.IsMatchAny(configuration.TestProjects, path);
        }

        if (Glob.IsMatchAny(configuration.NonTestProjects, path))
        {
            isTest = false;
        }

        return new ProjectDefinition(
            path,
            [.. references.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)],
            [.. analyzers.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)],
            isTest,
            isGenerator,
            [.. externals.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)]);
    }

    static IEnumerable<string> Values(XElement element, params string[] attributes) =>
        attributes
            .Select(attribute => (string?)element.Attribute(attribute))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(value => value!.Split(';', StringSplitOptions.RemoveEmptyEntries));

    static bool IsTrue(XElement element) => string.Equals(element.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase);

    static void AddExternal(string directory, string? value, List<string> externals)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var wildcard = value.IndexOf('*', StringComparison.Ordinal);
        var literal = wildcard < 0 ? value : value[..wildcard].Replace('\\', '/');
        var slash = literal.LastIndexOf('/');
        var resolvedFrom = literal;
        if (wildcard >= 0)
        {
            resolvedFrom = slash < 0 ? string.Empty : literal[..(slash + 1)];
        }

        var resolved = PathNormalizer.Resolve(directory, resolvedFrom);
        if (resolved is null)
        {
            return;
        }

        var inside = directory.Length == 0 || resolved.StartsWith(directory + "/", StringComparison.Ordinal);
        if (inside)
        {
            return;
        }

        externals.Add(wildcard < 0 || resolved.Length == 0 ? resolved : resolved + "/");
    }
}
