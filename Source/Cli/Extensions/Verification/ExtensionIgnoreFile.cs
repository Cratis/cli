// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Cli.Extensions.Verification;

/// <summary>
/// Names what a package folder holds that the build does not ship: sources, specifications, scripts. A folder is verified as it ships,
/// so everything it holds that the contract does not declare must be named here, in <c language="text">.extensionignore</c>.
/// </summary>
public class ExtensionIgnoreFile
{
    /// <summary>The name of the file, in the package folder.</summary>
    public const string FileName = ".extensionignore";

    static readonly string[] _always = [".git/", "node_modules/", "bin/", "obj/", ".ai-work/", FileName, "README*", "LICENSE*", "NOTICE*", "CHANGELOG*"];

    readonly List<Regex> _patterns;

    ExtensionIgnoreFile(IEnumerable<string> lines) =>
        _patterns = [.. _always.Concat(lines).Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith('#')).Select(ToPattern)];

    /// <summary>
    /// Reads the ignore file of a package folder; a folder without one ignores only what every package ignores.
    /// </summary>
    /// <param name="packageRoot">The package folder.</param>
    /// <returns>The ignore file.</returns>
    public static ExtensionIgnoreFile Read(string packageRoot)
    {
        var path = Path.Combine(packageRoot, FileName);
        return new(File.Exists(path) ? File.ReadAllLines(path) : []);
    }

    /// <summary>
    /// Gets a value indicating whether a package-relative path is ignored.
    /// </summary>
    /// <param name="relativePath">The path, with forward slashes.</param>
    /// <returns><see langword="true"/> when it is not shipped.</returns>
    public bool Ignores(string relativePath) => _patterns.Exists(pattern => pattern.IsMatch(relativePath));

    static Regex ToPattern(string line)
    {
        var directory = line.EndsWith('/');
        var body = line.TrimEnd('/').TrimStart('/');
        var escaped = Regex.Escape(body).Replace(@"\*\*", "\u0001", StringComparison.Ordinal).Replace(@"\*", "[^/]*", StringComparison.Ordinal).Replace("\u0001", ".*", StringComparison.Ordinal);
        var expression = directory ? $"^{escaped}/.*$" : $"^{escaped}(/.*)?$";
        return new(expression, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    }
}
