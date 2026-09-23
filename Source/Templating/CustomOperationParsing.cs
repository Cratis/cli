// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

using Cratis.Templating.Configuration;

namespace Cratis.Templating;

/// <summary>
/// Parses global and special custom operations with strict configuration-key validation per operation type.
/// </summary>
static class CustomOperationParsing
{
    /// <summary>
    /// Parses an array of custom operation definitions.
    /// </summary>
    /// <param name="elements">The operation elements.</param>
    /// <param name="context">Path context for errors.</param>
    /// <param name="glob">The glob scope, or null for global operations.</param>
    /// <returns>The parsed operations.</returns>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    /// <exception cref="UnsupportedTemplateConstruct">Thrown when the manifest uses a construct this engine does not implement.</exception>
    public static IReadOnlyList<CustomOperationConfig> ParseOperations(IReadOnlyList<JsonElement> elements, string context, string? glob)
    {
        var result = new List<CustomOperationConfig>();
        var index = 0;
        foreach (var element in elements)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidTemplateManifest($"{context}[{index}] must be an object.");
            }

            Json.RejectUnknownProperties(element, $"{context}[{index}]", "type", "condition", "configuration");
            var typeText = Json.GetString(element, "type")?.ToLowerInvariant()
                ?? throw new InvalidTemplateManifest($"{context}[{index}] is missing 'type'.");
            var type = typeText switch
            {
                "conditional" => CustomOperationType.Conditional,
                "replacement" => CustomOperationType.Replacement,
                "flag" => CustomOperationType.Flag,
                "include" => CustomOperationType.Include,
                "region" => CustomOperationType.Region,
                "expandvariables" => CustomOperationType.ExpandVariables,
                "balancednesting" => CustomOperationType.BalancedNesting,
                _ => throw new UnsupportedTemplateConstruct(
                    $"{context}[{index}].type",
                    $"operation type '{typeText}' is not one of balancedNesting, conditional, flag, include, region, replacement, expandVariables.")
            };

            var configuration = Json.GetObject(element, "configuration")
                ?? throw new InvalidTemplateManifest($"{context}[{index}] is missing 'configuration'.");
            result.Add(ParseOperationConfiguration(type, configuration, $"{context}[{index}].configuration") with { Glob = glob });
            index++;
        }
        return result;
    }

    /// <summary>
    /// Parses the <c language="csharp">specialCustomOperations</c> section, keyed by glob.
    /// </summary>
    /// <param name="root">The manifest root element.</param>
    /// <returns>Operations keyed by glob.</returns>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    public static IReadOnlyDictionary<string, IReadOnlyList<CustomOperationConfig>> ParseSpecialOperations(JsonElement root)
    {
        var result = new Dictionary<string, IReadOnlyList<CustomOperationConfig>>();
        foreach (var (glob, element) in Json.GetObjectProperties(root, "specialCustomOperations"))
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidTemplateManifest($"template.json: specialCustomOperations.{glob} must be an object.");
            }
            Json.RejectUnknownProperties(element, $"template.json: specialCustomOperations.{glob}", "flagPrefix", "operations");
            result[glob] = ParseOperations(Json.GetArray(element, "operations"), $"specialCustomOperations.{glob}", glob);
        }
        return result;
    }

    static CustomOperationConfig ParseOperationConfiguration(CustomOperationType type, JsonElement configuration, string context)
    {
        switch (type)
        {
            case CustomOperationType.Conditional:
                Json.RejectUnknownProperties(
                        configuration,
                        context,
                        "if",
                        "else",
                        "elseif",
                        "endif",
                        "actionableIf",
                        "actionableElse",
                        "actionableElseif",
                        "actions",
                        "trim",
                        "wholeLine",
                        "evaluator");
                return new CustomOperationConfig
                {
                    Type = type,
                    Tokens = ParseTokenMap(configuration, context, "if", "else", "elseif", "endif", "actionableIf", "actionableElse", "actionableElseif"),
                    Actions = Json.GetStringArray(configuration, "actions"),
                    Trim = Json.GetBool(configuration, "trim"),
                    WholeLine = Json.GetBool(configuration, "wholeLine"),
                    Evaluator = Json.GetString(configuration, "evaluator")
                };

            case CustomOperationType.Replacement:
                Json.RejectUnknownProperties(configuration, context, "variable", "replacement", "token");
                return new CustomOperationConfig
                {
                    Type = type,
                    Variable = Json.GetString(configuration, "variable"),
                    Replacement = Json.GetString(configuration, "replacement"),
                    Token = Json.GetString(configuration, "token")
                };

            case CustomOperationType.Flag:
                Json.RejectUnknownProperties(configuration, context, "token", "flagName");
                return new CustomOperationConfig
                {
                    Type = type,
                    Token = Json.GetString(configuration, "token")
                        ?? throw new InvalidTemplateManifest($"{context}: flag operation requires 'token'."),
                };

            case CustomOperationType.Include:
                Json.RejectUnknownProperties(configuration, context, "include", "token");
                return new CustomOperationConfig
                {
                    Type = type,
                    IncludePath = Json.GetString(configuration, "include"),
                    Token = Json.GetString(configuration, "token")
                        ?? throw new InvalidTemplateManifest($"{context}: include operation requires 'token'.")
                };

            case CustomOperationType.Region:
                Json.RejectUnknownProperties(configuration, context, "begin", "end", "regionName", "wholeLine", "trim");
                return new CustomOperationConfig
                {
                    Type = type,
                    Begin = Json.GetString(configuration, "begin"),
                    End = Json.GetString(configuration, "end"),
                    RegionName = Json.GetString(configuration, "regionName"),
                    WholeLine = Json.GetBool(configuration, "wholeLine"),
                    Trim = Json.GetBool(configuration, "trim")
                };

            case CustomOperationType.ExpandVariables:
                Json.RejectUnknownProperties(configuration, context, "prefix", "suffix");
                return new CustomOperationConfig
                {
                    Type = type,
                    Prefix = Json.GetString(configuration, "prefix") ?? "$(",
                    Suffix = Json.GetString(configuration, "suffix") ?? ")"
                };

            case CustomOperationType.BalancedNesting:
                Json.RejectUnknownProperties(configuration, context, "start", "end", "wholeLine", "trim");
                return new CustomOperationConfig
                {
                    Type = type,
                    Begin = Json.GetString(configuration, "start"),
                    End = Json.GetString(configuration, "end"),
                    WholeLine = Json.GetBool(configuration, "wholeLine"),
                    Trim = Json.GetBool(configuration, "trim")
                };

            default:
                throw new UnsupportedTemplateConstruct(context, $"operation type '{type}' is not implemented.");
        }
    }

    static Dictionary<string, IReadOnlyList<string>> ParseTokenMap(
        JsonElement configuration, string context, params string[] keys)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var key in keys)
        {
            if (configuration.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Array)
            {
                result[key] = [.. value.EnumerateArray().Select(item => item.GetString() ?? string.Empty)];
            }
        }
        return result;
    }
}
