// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// The placeholders a kit's prompt, learn prompt, verification commands and Git templates may use, and how the host fills
/// them from an order. A name in single braces is a placeholder; double braces and braces holding spaces, as in JSON or
/// code, are left alone.
/// </summary>
public static partial class BuildKitPlaceholders
{
    /// <summary>
    /// The names a host fills.
    /// </summary>
    public static readonly IReadOnlyList<string> Names =
        ["order", "outcome", "slice", "slices", "sliceId", "feature", "featurePath", "application", "part", "attempt", "conventions"];

    [GeneratedRegex(@"(?<!\{)\{(?<name>[A-Za-z][A-Za-z0-9]*)\}(?!\})", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Placeholder { get; }

    [GeneratedRegex(@"^[A-Za-z0-9._/:=+-]+$", RegexOptions.None, 1000)]
    private static partial Regex SafeForShell { get; }

    /// <summary>
    /// Finds the placeholders in a text that a host does not fill.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The unknown names, each once.</returns>
    public static IEnumerable<string> Unknown(string text) =>
        Placeholder.Matches(text).Select(match => match.Groups["name"].Value).Where(name => !Names.Contains(name)).Distinct(StringComparer.Ordinal);

    /// <summary>
    /// Gets the values of every placeholder for an order.
    /// </summary>
    /// <param name="order">The order.</param>
    /// <param name="orderPath">Where the host wrote the order.</param>
    /// <param name="outcomePath">Where the agent writes its outcome.</param>
    /// <param name="conventions">The project path of the recorded conventions, or empty.</param>
    /// <returns>The value of each placeholder.</returns>
    public static IReadOnlyDictionary<string, string> For(BuildOrder order, string orderPath, string outcomePath, string conventions) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["order"] = orderPath,
            ["outcome"] = outcomePath,
            ["slice"] = order.Slices.Count > 0 ? order.Slices[0].Name : string.Empty,
            ["slices"] = string.Join(", ", order.Slices.Select(slice => slice.Name)),
            ["sliceId"] = order.Slices.Count > 0 ? order.Slices[0].Id : string.Empty,
            ["feature"] = order.Feature,
            ["featurePath"] = string.Join('/', order.FeaturePath),
            ["application"] = order.Application,
            ["part"] = order.Part,
            ["attempt"] = order.Attempt.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["conventions"] = conventions
        };

    /// <summary>
    /// Fills a prompt or a Git template.
    /// </summary>
    /// <param name="template">The template.</param>
    /// <param name="values">The value of each placeholder.</param>
    /// <returns>The filled text.</returns>
    public static string Fill(string template, IReadOnlyDictionary<string, string> values) =>
        Placeholder.Replace(template, match => values.TryGetValue(match.Groups["name"].Value, out var value) ? value : match.Value);

    /// <summary>
    /// Fills a command, quoting every value that a shell would otherwise split or interpret.
    /// </summary>
    /// <param name="command">The command template.</param>
    /// <param name="values">The value of each placeholder.</param>
    /// <returns>The filled command.</returns>
    public static string FillCommand(string command, IReadOnlyDictionary<string, string> values) =>
        Placeholder.Replace(command, match => values.TryGetValue(match.Groups["name"].Value, out var value) ? Quote(value) : match.Value);

    static string Quote(string value)
    {
        if (value.Length > 0 && SafeForShell.IsMatch(value)) return value;
        return OperatingSystem.IsWindows() ? $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"" : $"'{value.Replace("'", "'\\''", StringComparison.Ordinal)}'";
    }
}
