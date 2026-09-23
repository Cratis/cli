// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cratis.Templating.Configuration;

namespace Cratis.Templating.ValueForms;

/// <summary>
/// Applies value form transforms — the built-in identifier set, the safe-name forms, and user-defined
/// forms composed through <c language="csharp">replace</c> and <c language="csharp">chain</c> steps. A form is addressed by name; user-defined
/// forms may reference other forms, and unknown names fail loudly.
/// </summary>
/// <param name="userForms">The userForms to use.</param>
public partial class ValueFormRegistry(IReadOnlyDictionary<string, ValueFormConfig> userForms)
{
    /// <summary>
    /// Gets the built-in value form names applied automatically to the <c language="csharp">sourceName</c> symbol.
    /// </summary>
    public static readonly string[] SourceNameDefaultForms =
    [
        "identity", "safe_name", "safe_namespace", "lower_safe_name", "lower_safe_namespace"
    ];

    /// <summary>
    /// The well-known value form names that can appear in a <c language="csharp">{-VALUE-FORMS-}</c> token suffix.
    /// </summary>
    public static readonly string[] WellKnownForms =
    [
        "identity", "lowerCase", "lowerCaseInvariant", "upperCase", "upperCaseInvariant",
        "firstLowerCase", "firstLowerCaseInvariant", "firstUpperCase", "firstUpperCaseInvariant",
        "titleCase", "kebabCase", "snakeCase", "xmlEncode", "jsonEncode",
        "safe_name", "lower_safe_name", "safe_namespace", "lower_safe_namespace"
    ];

    /// <summary>
    /// Gets the empty registry with no user-defined forms.
    /// </summary>
    public static ValueFormRegistry Empty { get; } = new(new Dictionary<string, ValueFormConfig>());

    [GeneratedRegex("[A-Z]+(?![a-z])|[A-Z][a-z0-9]*|[a-z0-9]+", RegexOptions.None, 2000)]
    private static partial Regex WordBoundaryRegex { get; }

    /// <summary>
    /// Applies a named form to a value.
    /// </summary>
    /// <param name="form">The form name.</param>
    /// <param name="value">The value to transform.</param>
    /// <returns>The transformed value.</returns>
    /// <exception cref="UnsupportedTemplateConstruct">Thrown when the manifest uses a construct this engine does not implement.</exception>
    public string Apply(string form, string value)
    {
        if (userForms.TryGetValue(form, out var userForm))
        {
            return ApplyUserForm(userForm, value, []);
        }

        return form switch
        {
            "identity" => value,
            "lowerCase" => value.ToLower(CultureInfo.CurrentCulture),
            "lowerCaseInvariant" => value.ToLowerInvariant(),
            "upperCase" => value.ToUpper(CultureInfo.CurrentCulture),
            "upperCaseInvariant" => value.ToUpperInvariant(),
            "firstLowerCase" => FirstCharCase(value, false, CultureInfo.CurrentCulture),
            "firstLowerCaseInvariant" => FirstCharCase(value, false, CultureInfo.InvariantCulture),
            "firstUpperCase" => FirstCharCase(value, true, CultureInfo.CurrentCulture),
            "firstUpperCaseInvariant" => FirstCharCase(value, true, CultureInfo.InvariantCulture),
            "titleCase" => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value),
            "kebabCase" => JoinWords(value, "-", lowercase: true),
            "snakeCase" => JoinWords(value, "_", lowercase: true),
            "xmlEncode" => System.Security.SecurityElement.Escape(value) ?? value,
            "jsonEncode" => JsonEncoded(value),
            "safe_name" => SafeName(value, '_'),
            "lower_safe_name" => SafeName(value, '_').ToLowerInvariant(),
            "safe_namespace" => SafeName(value, '.'),
            "lower_safe_namespace" => SafeName(value, '.').ToLowerInvariant(),
            _ => throw new UnsupportedTemplateConstruct(
                "forms",
                $"value form '{form}' is not defined. Known forms: {string.Join(", ", WellKnownForms)}{(userForms.Count > 0 ? $", plus user-defined: {string.Join(", ", userForms.Keys.Order())}" : string.Empty)}.")
        };
    }

    static string FirstCharCase(string value, bool upper, CultureInfo culture) => value.Length == 0
        ? value
        : (upper ? char.ToUpper(value[0], culture) : char.ToLower(value[0], culture)) + value[1..];
    static string JsonEncoded(string value)
    {
        var encoded = JsonEncodedText.Encode(value, JavaScriptEncoder.Default);
        return encoded.ToString();
    }

    /// <summary>
    /// Transforms a value into a safe identifier. Non-word characters are replaced with underscores within
    /// each dot-separated segment; segments that start with a digit — or are empty — get an underscore prefix.
    /// Segments are joined with <paramref name="separator"/>: '.' for namespaces, '_' for class names.
    /// </summary>
    /// <param name="value">The value to use.</param>
    /// <param name="separator">The separator to use.</param>
    static string SafeName(string value, char separator)
    {
        var segments = value.Split('.');
        var safeSegments = segments.Select(segment =>
        {
            var builder = new StringBuilder();
            foreach (var character in segment)
            {
                builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
            }
            var safe = builder.ToString();
            return safe.Length == 0 || char.IsDigit(safe[0]) ? $"_{safe}" : safe;
        });
        return string.Join(separator, safeSegments);
    }

    static string JoinWords(string value, string separator, bool lowercase)
    {
        var words = WordBoundaryRegex.Matches(value)
            .Select(match => match.Value)
            .Where(word => word.Length > 0)
            .Select(word => lowercase ? word.ToLowerInvariant() : word);
        return string.Join(separator, words);
    }
    string ApplyUserForm(ValueFormConfig form, string value, IReadOnlyList<string> chain)
    {
        if (chain.Contains(form.Identifier))
        {
            throw new InvalidTemplateManifest($"forms: chain cycle detected at '{form.Identifier}'.");
        }

        switch (form.Identifier)
        {
            case "replace":
                if (form.Pattern is null || form.Replacement is null)
                {
                    throw new InvalidTemplateManifest("forms: replace form requires 'pattern' and 'replacement'.");
                }
                return Regex.Replace(value, form.Pattern, form.Replacement, RegexOptions.None, TimeSpan.FromSeconds(2));

            case "chain":
                var steps = chain.Append(form.Identifier).ToArray();
                var result = value;
                foreach (var step in form.Steps)
                {
                    if (!userForms.TryGetValue(step, out var stepForm))
                    {
                        // A chain step may name a built-in form.
                        result = Apply(step, result);
                        continue;
                    }
                    result = ApplyUserForm(stepForm, result, steps);
                }
                return result;

            case var builtIn when WellKnownForms.Contains(builtIn):
                return Apply(form.Identifier, value);
            default:
                throw new UnsupportedTemplateConstruct(
                    "forms",
                    $"form identifier '{form.Identifier}' is not implemented. Known identifiers: replace, chain, {string.Join(", ", WellKnownForms)}.");
        }
    }
}
