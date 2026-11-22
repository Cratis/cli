// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace Cratis.Cli.for_RenderingDocumentation.given;

public class the_rendering_documentation : Specification
{
    protected string ScreenplayPage = string.Empty;
    protected string ScreenplayMcpPage = string.Empty;
    protected string RunPage = string.Empty;
    protected IDictionary<string, string> Versions = new Dictionary<string, string>();

    void Establish()
    {
        var root = FindRepositoryRoot();
        ScreenplayPage = File.ReadAllText(Path.Combine(root, "Documentation", "reference", "screenplay.md"));
        ScreenplayMcpPage = File.ReadAllText(Path.Combine(root, "Documentation", "reference", "screenplay-mcp.md"));
        RunPage = File.ReadAllText(Path.Combine(root, "Documentation", "reference", "run.md"));
        Versions = XDocument.Load(Path.Combine(root, "Directory.Packages.props"))
            .Descendants().Where(_ => _.Name.LocalName == "PropertyGroup")
            .SelectMany(_ => _.Elements())
            .ToDictionary(_ => _.Name.LocalName, _ => _.Value);
    }

    /// <summary>
    /// Gets the contents of the first fenced block of a language that follows a heading.
    /// </summary>
    /// <param name="page">The page to read.</param>
    /// <param name="heading">The heading line that opens the section.</param>
    /// <param name="language">The fence language.</param>
    /// <returns>The block contents.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the heading or the block is missing.</exception>
    protected static string FencedBlock(string page, string heading, string language)
    {
        var fence = "```" + language + "\n";
        var headingAt = page.IndexOf(heading, StringComparison.Ordinal);
        var start = headingAt < 0 ? -1 : page.IndexOf(fence, headingAt, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"No '{language}' block follows '{heading}'.");
        }

        start += fence.Length;
        return page[start..page.IndexOf("\n```", start, StringComparison.Ordinal)];
    }

    static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Directory.Packages.props was not found above the specification binaries.");
    }
}
