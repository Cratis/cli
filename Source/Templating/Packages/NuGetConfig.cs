// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;

namespace Cratis.Templating.Packages;

/// <summary>
/// One NuGet feed, with optional credentials resolved from the configuration and the environment.
/// </summary>
/// <param name="Name">The feed name.</param>
/// <param name="Url">The feed source URL.</param>
/// <param name="Username">The resolved username, when configured.</param>
/// <param name="Password">The resolved password or clear-text password, when configured.</param>
public record NuGetFeed(string Name, string Url, string? Username = null, string? Password = null);

/// <summary>
/// Discovers and parses NuGet configuration: machine-level, user-level and repository-level
/// <c language="csharp">NuGet.Config</c> files, honoring <c language="csharp">&lt;packageSources&gt;</c>, <c language="csharp">&lt;packageSourceCredentials&gt;</c>
/// (including base64 and clear-text values with <c language="csharp">%ENV%</c> expansion) and the disabled-sources list.
/// </summary>
public static partial class NuGetConfig
{
    [System.Text.RegularExpressions.GeneratedRegex("%(?<name>[^%]+)%", System.Text.RegularExpressions.RegexOptions.None, 2000)]
    private static partial System.Text.RegularExpressions.Regex PlaceholderRegex { get; }

    /// <summary>
    /// Discovers the effective feeds for a working directory: repository config walking up from it,
    /// then the user config, then machine config; nuget.org when nothing is configured.
    /// </summary>
    /// <param name="workingDirectory">The directory to resolve configuration for.</param>
    /// <returns>The effective feed list, in configuration order.</returns>
    public static IReadOnlyList<NuGetFeed> DiscoverFeeds(string workingDirectory)
    {
        var feeds = new List<NuGetFeed>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in ConfigPathCandidates(workingDirectory))
        {
            if (!File.Exists(path))
            {
                continue;
            }
            foreach (var feed in ParseFeeds(path))
            {
                if (seen.Add(feed.Url))
                {
                    feeds.Add(feed);
                }
            }
        }

        if (feeds.Count == 0)
        {
            feeds.Add(new NuGetFeed("nuget.org", "https://api.nuget.org/v3/index.json"));
        }
        return feeds;
    }

    /// <summary>
    /// Parses the feeds from one configuration file.
    /// </summary>
    /// <param name="path">The configuration file path.</param>
    /// <returns>The configured feeds that are not disabled.</returns>
    public static IReadOnlyList<NuGetFeed> ParseFeeds(string path)
    {
        var document = new XmlDocument();
        try
        {
            document.Load(path);
        }
        catch (XmlException)
        {
            return [];
        }

        var disabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in SelectNodes(document, "//disabledPackageSources/add"))
        {
            var key = node.Attributes?["key"]?.Value;
            if (key is not null && string.Equals(node.Attributes?["value"]?.Value, "true", StringComparison.Ordinal))
            {
                disabled.Add(key);
            }
        }

        var credentials = new Dictionary<string, (string? User, string? Password)>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in SelectNodes(document, "//packageSourceCredentials/*"))
        {
            credentials[source.Name] = (ReadTextCredential(source, "Username"), ReadPasswordCredential(source));
        }

        var feeds = new List<NuGetFeed>();
        foreach (var node in SelectNodes(document, "//packageSources/add"))
        {
            var name = node.Attributes?["key"]?.Value;
            var url = node.Attributes?["value"]?.Value;
            if (name is null || url is null || disabled.Contains(name))
            {
                continue;
            }
            var (user, password) = credentials.TryGetValue(name, out var credential) ? credential : (null, null);
            feeds.Add(new NuGetFeed(name, url, user, password));
        }
        return feeds;
    }

    static XmlNode[] SelectNodes(XmlDocument document, string xpath) =>
        document.SelectNodes(xpath) is { } nodes ? [.. nodes.Cast<XmlNode>()] : [];

    static string? ReadTextCredential(XmlNode source, string name) =>
        source.SelectSingleNode(name) is { } node ? ExpandEnvironment(node.Attributes?["value"]?.Value) : null;

    static string? ReadPasswordCredential(XmlNode source)
    {
        if (source.SelectSingleNode("ClearTextPassword") is { } clearNode)
        {
            return ExpandEnvironment(clearNode.Attributes?["value"]?.Value);
        }

        if (source.SelectSingleNode("Password") is not { } encodedNode)
        {
            return null;
        }

        var value = encodedNode.Attributes?["value"]?.Value;
        if (value is null)
        {
            return null;
        }

        try
        {
            return ExpandEnvironment(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value)));
        }
        catch (FormatException)
        {
            return ExpandEnvironment(value);
        }
    }

    static string? ExpandEnvironment(string? value) =>
        value is null ? null : PlaceholderRegex.Replace(value, match =>
            Environment.GetEnvironmentVariable(match.Groups["name"].Value) ?? match.Value);

    static IEnumerable<string> ConfigPathCandidates(string workingDirectory)
    {
        // Repository level: walk up from the working directory.
        var directory = new DirectoryInfo(Path.GetFullPath(workingDirectory));
        while (directory is not null)
        {
            foreach (var name in new[] { "nuget.config", "NuGet.Config" })
            {
                yield return Path.Combine(directory.FullName, name);
            }
            directory = directory.Parent;
        }

        // User level.
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget",
            "NuGet",
            "NuGet.Config");

        // Machine level.
        if (!OperatingSystem.IsWindows())
        {
            yield return "/etc/nuget/NuGet.Config";
        }
    }
}
