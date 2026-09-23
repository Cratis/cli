// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating;
using Cratis.Templating.Configuration;

namespace Cratis.Cli.Templates;

/// <summary>
/// The result of resolving the <c language="csharp">--database</c> selection: values merged into the bound template
/// parameters, and any errors — empty when the selection applies cleanly.
/// </summary>
/// <param name="Merge">Parameter values to merge into the bound set, keyed by parameter name.</param>
/// <param name="Errors">Resolution errors, empty on success.</param>
public record DatabaseSelectionResult(IReadOnlyDictionary<string, string> Merge, IReadOnlyList<string> Errors)
{
    /// <summary>
    /// Gets the empty result — nothing merged, nothing wrong.
    /// </summary>
    public static DatabaseSelectionResult None { get; } = new(new Dictionary<string, string>(), []);
}

/// <summary>
/// Resolves the <c language="csharp">--database</c> option into the template's <c language="csharp">Database</c> parameter value, so
/// templates use it like any parameter — in conditions, replacements and switch symbols. The four
/// supported backends match case-insensitively; when the template declares choices, the value is
/// injected in the choice's canonical casing. Absent templates (no <c language="csharp">Database</c> parameter)
/// pass through untouched unless the option was passed explicitly, which is an error rather than
/// a silent drop.
/// </summary>
public static class DatabaseSelection
{
    /// <summary>
    /// Gets the backend applied when the option is not passed.
    /// </summary>
    public const string Default = "mongodb";

    /// <summary>
    /// Gets the supported database backends, in their canonical lower-case forms.
    /// </summary>
    public static readonly string[] Supported =
    [
        "mongodb", "postgresql", "mssql", "sqlite"
    ];

    /// <summary>
    /// Determines whether a requested backend is supported, case-insensitively.
    /// </summary>
    /// <param name="database">The requested backend.</param>
    /// <returns>True when the backend is one of the supported values.</returns>
    public static bool IsSupported(string database) =>
        Supported.Any(candidate => candidate.Equals(database, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Resolves the selection against a template manifest and the parameters already bound from
    /// the raw command line.
    /// </summary>
    /// <param name="manifest">The template manifest.</param>
    /// <param name="requested">The requested backend, or null when the option was not passed.</param>
    /// <param name="bound">Parameter values already bound from raw template arguments.</param>
    /// <returns>The merge set and any errors.</returns>
    public static DatabaseSelectionResult Resolve(
        TemplateConfig manifest,
        string? requested,
        IReadOnlyDictionary<string, string> bound)
    {
        var database = requested ?? Default;
        var normalized = Supported.FirstOrDefault(candidate => candidate.Equals(database, StringComparison.OrdinalIgnoreCase));
        if (normalized is null)
        {
            return Error($"--database '{database}' is not supported. Supported databases: {string.Join(", ", Supported)}.");
        }

        if (!manifest.Symbols.TryGetValue("Database", out var symbol) || symbol.Type != SymbolType.Parameter)
        {
            return requested is null
                ? DatabaseSelectionResult.None
                : Error($"template '{manifest.ShortName}' does not support --database selection — it declares no Database parameter.");
        }

        // A raw --Database argument and the --database option must agree; the explicit conflict is
        // an error, never a silent override. Through the command line this cannot arise today —
        // the CLI framework matches long options case-insensitively, so --Database is consumed by
        // the --database option itself — but the resolver is a public helper and stays guarded.
        if (bound.TryGetValue("Database", out var rawValue)
            && !string.Equals(rawValue, database, StringComparison.OrdinalIgnoreCase))
        {
            return Error($"--database '{database}' conflicts with the --Database '{rawValue}' argument — pass one of the two.");
        }

        if (symbol.Choices.Count == 0)
        {
            // Free-form parameters receive the canonical lower-case form.
            return new DatabaseSelectionResult(
                new Dictionary<string, string> { ["Database"] = normalized },
                []);
        }

        var choice = symbol.Choices.FirstOrDefault(candidate =>
            candidate.Choice.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        return choice is null
            ? Error($"--database '{database}' is not offered by template '{manifest.ShortName}'. Offered databases: {string.Join(", ", symbol.Choices.Select(c => c.Choice))}.")
            : new DatabaseSelectionResult(new Dictionary<string, string> { ["Database"] = choice.Choice }, []);
    }

    static DatabaseSelectionResult Error(string message) =>
        new(new Dictionary<string, string>(), [message]);
}
