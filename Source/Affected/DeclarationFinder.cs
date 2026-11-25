// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Affected;

/// <summary>
/// Finds the source files that declare each class, so a change to a test class selects that test even when coverage did not record it.
/// </summary>
public static class DeclarationFinder
{
    /// <summary>
    /// Finds the declaring files of the classes in source text.
    /// </summary>
    /// <param name="file">The repository relative path, recorded as the declaring file.</param>
    /// <param name="source">The C# source text.</param>
    /// <returns>The fully qualified class names, with + separating nested types.</returns>
    public static IReadOnlyList<(string Class, string File)> Find(string file, string source)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var found = new List<(string, string)>();
        Collect(root, string.Empty, file, found);
        return found;
    }

    /// <summary>
    /// Finds the declaring files of every class in the C# files of a project.
    /// </summary>
    /// <param name="repositoryRoot">The working tree root.</param>
    /// <param name="project">The project.</param>
    /// <param name="trackedFiles">The tracked files of the repository.</param>
    /// <returns>For each class, the files that declare it; partial classes have several.</returns>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> FindInProject(string repositoryRoot, ProjectDefinition project, IEnumerable<string> trackedFiles)
    {
        var declarations = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var file in trackedFiles.Where(file => file.EndsWith(".cs", StringComparison.Ordinal) && project.Contains(file)))
        {
            var path = Path.Combine(repositoryRoot, file);
            if (!File.Exists(path))
            {
                continue;
            }

            foreach (var (@class, declaringFile) in Find(file, File.ReadAllText(path)))
            {
                if (!declarations.TryGetValue(@class, out var files))
                {
                    declarations[@class] = files = [];
                }

                files.Add(declaringFile);
            }
        }

        return declarations.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<string>)[.. pair.Value.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)], StringComparer.Ordinal);
    }

    static void Collect(SyntaxNode node, string prefix, string file, List<(string, string)> found)
    {
        foreach (var child in node.ChildNodes())
        {
            switch (child)
            {
                case BaseNamespaceDeclarationSyntax ns:
                    Collect(ns, Join(prefix, ns.Name.ToString(), '.'), file, found);
                    break;
                case TypeDeclarationSyntax type:
                    var name = Join(prefix, type.Identifier.Text, prefix.Length > 0 && IsNestedPrefix(node) ? '+' : '.');
                    found.Add((name, file));
                    Collect(type, name, file, found);
                    break;
                default:
                    Collect(child, prefix, file, found);
                    break;
            }
        }
    }

    static bool IsNestedPrefix(SyntaxNode parent) => parent is TypeDeclarationSyntax;

    static string Join(string prefix, string name, char separator) => prefix.Length == 0 ? name : prefix + separator + name;
}
