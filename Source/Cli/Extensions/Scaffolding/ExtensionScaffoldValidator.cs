// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;

namespace Cratis.Cli.Extensions.Scaffolding;

/// <summary>
/// Checks a scaffold request, so a wizard answer and a flag are held to the same rules.
/// </summary>
public static partial class ExtensionScaffoldValidator
{
    [GeneratedRegex("^[A-Z][A-Za-z0-9]*$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex NamePattern { get; }

    /// <summary>
    /// Validates a request and normalizes the case of its choices.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The normalized request and every error found.</returns>
    public static (ExtensionScaffoldRequest Request, IReadOnlyList<string> Errors) Validate(ExtensionScaffoldRequest request)
    {
        var errors = new List<string>();
        var normalized = request with
        {
            Backend = request.Backend.Trim().ToLowerInvariant(),
            Frontend = request.Frontend.Trim().ToLowerInvariant(),
            Component = request.Component.Trim().ToLowerInvariant()
        };

        if (!NamePattern.IsMatch(normalized.Name))
        {
            errors.Add($"The name '{normalized.Name}' must be a PascalCase identifier of letters and digits, such as 'Stripe' or 'OrderTracking'.");
        }

        if (normalized.Kind == ExtensionScaffoldKind.Extension)
        {
            Choice(errors, "backend", normalized.Backend, ExtensionScaffoldVocabulary.Backends);
            Choice(errors, "frontend", normalized.Frontend, ExtensionScaffoldVocabulary.Frontends);
            if (normalized.Component != ExtensionScaffoldVocabulary.None)
            {
                errors.Add("A full extension has no --component; choose its --frontend instead.");
            }
        }
        else
        {
            if (normalized.Backend != ExtensionScaffoldVocabulary.None || normalized.Frontend != ExtensionScaffoldVocabulary.None)
            {
                errors.Add("A frontend-only package has no backend and no --frontend; choose its --component (react or mount).");
            }

            if (normalized.Component == ExtensionScaffoldVocabulary.None)
            {
                errors.Add($"A frontend-only package needs a --component: {string.Join(", ", ExtensionScaffoldVocabulary.Components)}.");
            }
            else if (!ExtensionScaffoldVocabulary.Components.Contains(normalized.Component))
            {
                errors.Add($"Unknown component '{normalized.Component}'. Choose one of: {string.Join(", ", ExtensionScaffoldVocabulary.Components)}.");
            }
        }

        return (normalized, errors);
    }

    /// <summary>
    /// Spells a PascalCase name out as words, so <c language="text">OrderTracking</c> becomes <c language="text">Order Tracking</c>.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The words.</returns>
    public static string SpellOut(string name)
    {
        var builder = new StringBuilder();
        for (var index = 0; index < name.Length; index++)
        {
            if (index > 0 && char.IsUpper(name[index]) && !char.IsUpper(name[index - 1])) builder.Append(' ');
            builder.Append(name[index]);
        }

        return builder.ToString();
    }

    static void Choice(List<string> errors, string what, string value, IEnumerable<string> allowed)
    {
        if (value != ExtensionScaffoldVocabulary.None && !allowed.Contains(value))
        {
            errors.Add($"Unknown {what} '{value}'. Choose one of: {string.Join(", ", allowed)}, or none.");
        }
    }
}
