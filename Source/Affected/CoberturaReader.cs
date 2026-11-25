// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace Cratis.Affected;

/// <summary>
/// Reads the source files a Cobertura coverage report says were executed.
/// </summary>
public static class CoberturaReader
{
    /// <summary>
    /// Gets the repository files with executed code in a report.
    /// </summary>
    /// <param name="xml">The Cobertura report.</param>
    /// <param name="repositoryRoot">The absolute working tree root.</param>
    /// <param name="trackedFiles">The repository relative files tracked by git; anything else, such as generated code, is dropped.</param>
    /// <returns>Repository relative paths with forward slashes.</returns>
    public static IReadOnlySet<string> Files(string xml, string repositoryRoot, IReadOnlySet<string> trackedFiles)
    {
        var document = XDocument.Parse(xml);
        var sources = document.Descendants().Where(element => element.Name.LocalName == "source").Select(element => element.Value.Trim()).Where(value => value.Length > 0).ToList();
        var root = Path.GetFullPath(repositoryRoot).TrimEnd('/', '\\');
        var found = new HashSet<string>(StringComparer.Ordinal);

        foreach (var @class in document.Descendants().Where(element => element.Name.LocalName == "class"))
        {
            var fileName = (string?)@class.Attribute("filename");
            if (string.IsNullOrWhiteSpace(fileName) || !@class.Descendants().Any(IsExecutedLine))
            {
                continue;
            }

            foreach (var candidate in Candidates(fileName, sources, root))
            {
                var full = Path.GetFullPath(candidate).Replace('\\', '/');
                var prefix = root.Replace('\\', '/') + "/";
                if (full.StartsWith(prefix, StringComparison.Ordinal) && trackedFiles.Contains(full[prefix.Length..]))
                {
                    found.Add(full[prefix.Length..]);
                    break;
                }
            }
        }

        return found;
    }

    /// <summary>
    /// A report lists every instrumented class; only lines that ran at least once say a file was executed.
    /// </summary>
    /// <param name="element">A report element.</param>
    /// <returns>True for a line with at least one hit.</returns>
    static bool IsExecutedLine(XElement element) =>
        element.Name.LocalName == "line" && long.TryParse((string?)element.Attribute("hits"), System.Globalization.CultureInfo.InvariantCulture, out var hits) && hits > 0;

    static IEnumerable<string> Candidates(string fileName, List<string> sources, string root)
    {
        if (Path.IsPathRooted(fileName))
        {
            yield return fileName;
            yield break;
        }

        foreach (var source in sources)
        {
            yield return Path.Combine(source, fileName);
        }

        yield return Path.Combine(root, fileName);
    }
}
