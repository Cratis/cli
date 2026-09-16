// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cratis.Templating.Expressions;

namespace Cratis.Templating.Generators;

/// <summary>
/// Computes generated symbol values for all twelve documented generators: casing, coalesce, constant,
/// guid, join, now, port, random, regex, regexMatch, switch and evaluate. Unknown generators fail loudly.
/// </summary>
public static class GeneratorRunner
{
    /// <summary>
    /// Runs a generator for a symbol.
    /// </summary>
    /// <param name="generator">The generator name.</param>
    /// <param name="parameters">The generator parameters.</param>
    /// <param name="scope">Currently resolved symbol values.</param>
    /// <param name="symbolName">The symbol being generated, for error messages.</param>
    /// <returns>The generated value.</returns>
    /// <exception cref="UnsupportedTemplateConstruct">Thrown when the manifest uses a construct this engine does not implement.</exception>
    public static string Run(
        string generator,
        IReadOnlyDictionary<string, JsonElement> parameters,
        IReadOnlyDictionary<string, string> scope,
        string symbolName) => generator.ToLowerInvariant() switch
        {
            "constant" => parameters.TryGetValue("value", out var value) ? GetString(value, "value", symbolName) : string.Empty,
            "guid" => RunGuid(parameters),
            "now" => RunNow(parameters),
            "random" => RunRandom(parameters, symbolName),
            "port" => RunPort(parameters),
            "casing" => RunCasing(parameters, scope, symbolName),
            "coalesce" => RunCoalesce(parameters, scope),
            "join" => RunJoin(parameters, scope),
            "regex" => RunRegex(parameters, scope, symbolName),
            "regexmatch" => RunRegexMatch(parameters, scope, symbolName),
            "switch" => RunSwitch(parameters, scope, symbolName),
            "evaluate" => RunEvaluate(parameters, scope, symbolName),
            _ => throw new UnsupportedTemplateConstruct(
                $"symbols.{symbolName}.generator",
                $"generator '{generator}' is not implemented. Known generators: casing, coalesce, constant, guid, join, now, port, random, regex, regexMatch, switch, evaluate.")
        };

    static string RunGuid(IReadOnlyDictionary<string, JsonElement> parameters)
    {
        var format = parameters.TryGetValue("format", out var formatElement) ? formatElement.GetString() : "D";
        var guid = Guid.NewGuid();
        return format?.ToUpperInvariant() switch
        {
            "N" => guid.ToString("N"),
            "D" => guid.ToString("D"),
            "B" => guid.ToString("B"),
            "P" => guid.ToString("P"),
            "X" => guid.ToString("X"),
            _ => throw new UnsupportedTemplateConstruct("guid.format", $"GUID format '{format}' is not one of N, D, B, P, X.")
        };
    }

    static string RunNow(IReadOnlyDictionary<string, JsonElement> parameters)
    {
        var format = parameters.TryGetValue("format", out var formatElement) ? formatElement.GetString() : null;
        var kind = parameters.TryGetValue("avoidUtcOffset", out var avoidElement) && avoidElement.GetBoolean()
            ? DateTimeKind.Local
            : DateTimeKind.Utc;
        var now = new DateTime(DateTime.Now.Ticks, kind);
        return format is null ? now.ToString("yyyy-MM-dd") : now.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
    }

    static string RunRandom(IReadOnlyDictionary<string, JsonElement> parameters, string symbolName)
    {
        var type = parameters.TryGetValue("type", out var typeElement) ? typeElement.GetString()?.ToLowerInvariant() : "guid";
        switch (type)
        {
            case "guid":
                return Guid.NewGuid().ToString("D");

            case "numeric":
                var low = GetInt(parameters, "low", 0);
                var high = GetInt(parameters, "high", int.MaxValue);
                if (high <= low)
                {
                    throw new InvalidTemplateManifest($"symbols.{symbolName}: random numeric generator requires high > low.");
                }
                return Random.Shared.Next(low, high + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

            default:
                throw new UnsupportedTemplateConstruct(
                    $"symbols.{symbolName}.parameters.type",
                    $"random type '{type}' is not one of guid, numeric.");
        }
    }

    static string RunPort(IReadOnlyDictionary<string, JsonElement> parameters)
    {
        var low = GetInt(parameters, "low", 1024);
        var high = GetInt(parameters, "high", 65535);
        var fallback = GetInt(parameters, "fallback", 0);
        try
        {
            var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port >= low && port <= high
                ? port.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : fallback.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (SocketException)
        {
            return fallback.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    static string RunCasing(IReadOnlyDictionary<string, JsonElement> parameters, IReadOnlyDictionary<string, string> scope, string symbolName)
    {
        var source = parameters.TryGetValue("parameter", out var parameterElement)
            ? parameterElement.GetString()
            : throw new InvalidTemplateManifest($"symbols.{symbolName}: casing generator requires 'parameter'.");
        if (!scope.TryGetValue(source!, out var value))
        {
            throw new SymbolResolutionError($"symbols.{symbolName}: casing generator references unresolved symbol '{source}'.");
        }
        var toLower = parameters.TryGetValue("toLower", out var toLowerElement) && toLowerElement.GetBoolean();
        return toLower ? value.ToLowerInvariant() : value.ToUpperInvariant();
    }

    static string RunCoalesce(IReadOnlyDictionary<string, JsonElement> parameters, IReadOnlyDictionary<string, string> scope)
    {
        if (parameters.TryGetValue("sourceVariable", out var sourceElement)
            && scope.TryGetValue(sourceElement.GetString()!, out var value)
            && !string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (parameters.TryGetValue("fallbackVariable", out var fallbackElement)
            && scope.TryGetValue(fallbackElement.GetString()!, out var fallbackValue)
            && !string.IsNullOrEmpty(fallbackValue))
        {
            return fallbackValue;
        }

        return parameters.TryGetValue("defaultValue", out var defaultElement) ? GetString(defaultElement, "defaultValue", "coalesce") : string.Empty;
    }

    static string RunJoin(IReadOnlyDictionary<string, JsonElement> parameters, IReadOnlyDictionary<string, string> scope)
    {
        var symbolNames = parameters.TryGetValue("symbols", out var symbolsElement) && symbolsElement.ValueKind == JsonValueKind.Array
            ? symbolsElement.EnumerateArray().Select(item => item.GetString()!).ToArray()
            : [];
        var separator = parameters.TryGetValue("separator", out var separatorElement) ? separatorElement.GetString() : string.Empty;
        var values = symbolNames
            .Select(name => scope.TryGetValue(name, out var value) ? value : string.Empty)
            .Where(value => !string.IsNullOrEmpty(value))
            .ToArray();
        if (values.Length == 0)
        {
            return parameters.TryGetValue("fallbackValue", out var fallbackElement) ? GetString(fallbackElement, "fallbackValue", "join") : string.Empty;
        }
        return string.Join(separator, values);
    }

    static string RunRegex(IReadOnlyDictionary<string, JsonElement> parameters, IReadOnlyDictionary<string, string> scope, string symbolName)
    {
        var sourceName = parameters.TryGetValue("sourceVariable", out var sourceElement)
            ? sourceElement.GetString()
            : throw new InvalidTemplateManifest($"symbols.{symbolName}: regex generator requires 'sourceVariable'.");
        var pattern = parameters.TryGetValue("pattern", out var patternElement)
            ? patternElement.GetString()
            : throw new InvalidTemplateManifest($"symbols.{symbolName}: regex generator requires 'pattern'.");
        var replacement = parameters.TryGetValue("replacement", out var replacementElement) ? replacementElement.GetString() : string.Empty;
        if (!scope.TryGetValue(sourceName!, out var value))
        {
            throw new SymbolResolutionError($"symbols.{symbolName}: regex generator references unresolved symbol '{sourceName}'.");
        }
        return Regex.Replace(value, pattern!, replacement ?? string.Empty, RegexOptions.None, TimeSpan.FromSeconds(2));
    }

    static string RunRegexMatch(IReadOnlyDictionary<string, JsonElement> parameters, IReadOnlyDictionary<string, string> scope, string symbolName)
    {
        var sourceName = parameters.TryGetValue("source", out var sourceElement)
            ? sourceElement.GetString()
            : null;
        var pattern = parameters.TryGetValue("pattern", out var patternElement)
            ? patternElement.GetString()
            : throw new InvalidTemplateManifest($"symbols.{symbolName}: regexMatch generator requires 'pattern'.");
        var value = sourceName is not null && scope.TryGetValue(sourceName, out var sourceValue) ? sourceValue : string.Empty;
        var match = Regex.Match(value, pattern!, RegexOptions.None, TimeSpan.FromSeconds(2));
        if (!match.Success)
        {
            var optional = parameters.TryGetValue("optional", out var optionalElement) && optionalElement.GetBoolean();
            if (optional)
            {
                return string.Empty;
            }
            throw new SymbolResolutionError(
                $"symbols.{symbolName}: regexMatch pattern '{pattern}' did not match value '{value}'.");
        }

        if (parameters.TryGetValue("groupNumber", out var groupElement) && groupElement.ValueKind == JsonValueKind.Number)
        {
            return match.Groups[groupElement.GetInt32()].Value;
        }
        return match.Value;
    }

    static string RunSwitch(IReadOnlyDictionary<string, JsonElement> parameters, IReadOnlyDictionary<string, string> scope, string symbolName)
    {
        var dialect = ExpressionEvaluator.DialectFromName(
            parameters.TryGetValue("evaluator", out var evaluatorElement) ? evaluatorElement.GetString() : "C++2");
        if (!parameters.TryGetValue("cases", out var casesElement) || casesElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidTemplateManifest($"symbols.{symbolName}: switch generator requires 'cases'.");
        }

        foreach (var caseElement in casesElement.EnumerateArray())
        {
            if (caseElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidTemplateManifest($"symbols.{symbolName}: switch cases must be objects.");
            }
            var condition = caseElement.TryGetProperty("condition", out var conditionElement)
                ? conditionElement.GetString()
                : throw new InvalidTemplateManifest($"symbols.{symbolName}: switch case is missing 'condition'.");
            if (ExpressionEvaluator.EvaluateBoolean(condition!, dialect, scope))
            {
                return caseElement.TryGetProperty("value", out var valueElement)
                    ? valueElement.GetString() ?? string.Empty
                    : string.Empty;
            }
        }

        return parameters.TryGetValue("defaultValue", out var defaultElement) ? GetString(defaultElement, "defaultValue", symbolName) : string.Empty;
    }

    static string RunEvaluate(IReadOnlyDictionary<string, JsonElement> parameters, IReadOnlyDictionary<string, string> scope, string symbolName)
    {
        var dialect = ExpressionEvaluator.DialectFromName(
            parameters.TryGetValue("evaluator", out var evaluatorElement) ? evaluatorElement.GetString() : "C++2");
        var expression = parameters.TryGetValue("expression", out var expressionElement)
            ? expressionElement.GetString()
            : throw new InvalidTemplateManifest($"symbols.{symbolName}: evaluate generator requires 'expression'.");
        return ExpressionEvaluator.Evaluate(expression!, dialect, scope);
    }

    static string GetString(JsonElement element, string name, string symbolName) => element.ValueKind == JsonValueKind.String
        ? element.GetString()!
        : throw new InvalidTemplateManifest($"symbols.{symbolName}: parameter '{name}' must be a string.");

    static int GetInt(IReadOnlyDictionary<string, JsonElement> parameters, string name, int fallback) =>
        parameters.TryGetValue(name, out var element) && element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value)
            ? value
            : fallback;
}
