// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.CheckRelease;

/// <summary>
/// Renders a <see cref="CompatibilityResult"/> as the shared cratis.compat/v1 document or as a plain summary.
/// </summary>
public static class ReleaseOutput
{
    /// <summary>
    /// The id of the shared result format.
    /// </summary>
    public const string Format = "cratis.compat/v1";

    /// <summary>
    /// Writes a result in the given output format.
    /// </summary>
    /// <param name="format">The resolved output format.</param>
    /// <param name="result">The result to write.</param>
    public static void Write(string format, CompatibilityResult result) =>
        OutputFormatter.WriteObject(format, ToDocument(result), document => WriteText(format, result));

    /// <summary>
    /// Builds the JSON document. Required fields are always present as strings; optional provenance is present only when the release supplied it.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <returns>The document.</returns>
    public static object ToDocument(CompatibilityResult result) => new
    {
        schema = Format,
        format = Format,
        subject = new
        {
            repo = result.Release.Repository,
            release = result.Release.Release,
            component = result.Release.Component
        },
        production = new
        {
            environment = result.Release.Environment,
            readAt = result.ReadAt.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture)
        },
        eventStore = result.EventStore,
        verdict = result.Verdict,
        checks = result.Checks.Select(check => new
        {
            check = check.Check,
            expected = check.Expected,
            actual = check.Actual,
            verdict = check.Verdict,
            action = check.Action
        }),
        release = result.Release.Release
    };

    static void WriteText(string format, CompatibilityResult result)
    {
        var plain = string.Equals(format, OutputFormats.Plain, StringComparison.Ordinal);
        WriteLine(plain, $"Release {result.Release.Release}: {result.Verdict}");
        foreach (var check in result.Checks)
        {
            WriteLine(plain, $"  {check.Check}: {check.Verdict}");
            WriteLine(plain, $"    expected: {check.Expected}");
            WriteLine(plain, $"    actual:   {check.Actual}");
            if (check.Action.Length > 0)
            {
                WriteLine(plain, $"    action:   {check.Action}");
            }
        }
    }

    static void WriteLine(bool plain, string text)
    {
        if (plain)
        {
            Console.WriteLine(text);
        }
        else
        {
            AnsiConsole.WriteLine(text);
        }
    }
}
