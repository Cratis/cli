// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating;
using Cratis.Templating.Configuration;

namespace Cratis.Cli.Templates;

/// <summary>
/// The result of binding command-line arguments to a template's parameters.
/// </summary>
/// <param name="Values">Bound parameter values keyed by symbol name.</param>
/// <param name="Errors">Binding errors, empty on success.</param>
public record ParameterBindingResult(IReadOnlyDictionary<string, string> Values, IReadOnlyList<string> Errors);

/// <summary>
/// Binds the dynamic template parameters from the raw remaining arguments to the template's parameter
/// symbols: choice validation (including multi-value repetition joined with a pipe), boolean switches,
/// <c language="csharp">defaultIfOptionWithoutValue</c> and defaults. An unknown parameter is an error naming the valid
/// set — never a silent drop.
/// </summary>
public static class TemplateParameterBinder
{
    /// <summary>
    /// Binds raw arguments to parameter symbols.
    /// </summary>
    /// <param name="manifest">The template manifest.</param>
    /// <param name="rawArguments">The unrecognized raw arguments from the command context.</param>
    /// <returns>The binding result.</returns>
    public static ParameterBindingResult Bind(TemplateConfig manifest, IReadOnlyList<string> rawArguments)
    {
        var parameters = manifest.Symbols.Values
            .Where(symbol => symbol.Type == SymbolType.Parameter)
            .ToArray();
        var errors = new List<string>();
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var nameByOption = new Dictionary<string, SymbolConfig>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in parameters)
        {
            nameByOption[$"--{parameter.Name}"] = parameter;
        }

        for (var index = 0; index < rawArguments.Count; index++)
        {
            var argument = rawArguments[index];
            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                errors.Add($"'{argument}' is not recognized — template parameters are passed as --<Name> <value>.");
                continue;
            }

            var (option, inlineValue) = SplitInline(argument);
            if (!nameByOption.TryGetValue(option, out var parameter))
            {
                errors.Add(
                    $"unknown parameter '{option}' for template '{manifest.ShortName}'. Valid parameters: {string.Join(", ", parameters.Select(p => $"--{p.Name}"))}.");
                continue;
            }

            var value = inlineValue;
            if (value is null && index + 1 < rawArguments.Count && !rawArguments[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                value = rawArguments[++index];
            }

            if (value is null)
            {
                // Flag without value: booleans use their defaultIfOptionWithoutValue or true.
                value = parameter.DefaultIfOptionWithoutValue
                    ?? (string.Equals(parameter.DataType, "bool", StringComparison.Ordinal) || string.Equals(parameter.DataType, "bool?", StringComparison.Ordinal) ? "true" : null);
                if (value is null)
                {
                    errors.Add($"parameter '{parameter.Name}' requires a value.");
                    continue;
                }
            }

            var (valueErrors, normalized) = ValidateAndNormalize(parameter, value, values);
            if (valueErrors.Count > 0)
            {
                errors.AddRange(valueErrors);
                continue;
            }
            values[parameter.Name] = normalized!;
        }

        return new ParameterBindingResult(values, errors);
    }

    static (List<string> Errors, string? Normalized) ValidateAndNormalize(
        SymbolConfig parameter,
        string value,
        Dictionary<string, string> current)
    {
        if (parameter.DataType != "choice")
        {
            return ([], value);
        }

        var choices = parameter.Choices.Select(choice => choice.Choice).ToArray();
        var parts = value.Split(['|'], StringSplitOptions.RemoveEmptyEntries);
        var invalid = parts.Where(part => !choices.Contains(part, StringComparer.Ordinal)).ToArray();
        if (invalid.Length > 0)
        {
            return ([
                $"invalid value '{string.Join('|', invalid)}' for parameter '{parameter.Name}'. Valid choices: {string.Join(", ", choices)}."
            ], null);
        }

        // Repeating a multi-value parameter accumulates with the pipe separator.
        if (parameter.AllowMultipleValues
            && current.TryGetValue(parameter.Name, out var existing)
            && !string.IsNullOrEmpty(existing))
        {
            return ([], $"{existing}|{value}");
        }

        if (!parameter.AllowMultipleValues && parts.Length > 1)
        {
            return ([ $"parameter '{parameter.Name}' does not accept multiple values."], null);
        }
        return ([], value);
    }

    static (string Option, string? InlineValue) SplitInline(string argument)
    {
        var separator = argument.IndexOf('=', StringComparison.Ordinal);
        return separator > 0
            ? (argument[..separator], argument[(separator + 1)..])
            : (argument, null);
    }
}
