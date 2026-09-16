// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

using Cratis.Templating.Configuration;

namespace Cratis.Templating;

/// <summary>
/// Symbol parsing for all five symbol types, with strict property validation.
/// </summary>
static class SymbolParsing
{
    static readonly string[] _parameterProperties =
    [
        "type", "datatype", "dataType", "choices", "defaultValue", "defaultIfOptionWithoutValue",
        "description", "displayName", "prompt", "replaces", "fileRename", "isEnabled", "isRequired",
        "allowMultipleValues", "enableQuotelessLiterals", "forms", "onlyIf"
    ];

    static readonly string[] _derivedProperties =
    [
        "type", "datatype", "valueSource", "valueTransform", "replaces", "fileRename", "description", "isEnabled", "isRequired", "forms"
    ];

    static readonly string[] _computedProperties = ["type", "datatype", "value", "evaluator", "replaces", "fileRename", "description", "isEnabled", "isRequired", "forms"];

    static readonly string[] _generatedProperties =
    [
        "type", "datatype", "generator", "parameters", "replaces", "fileRename", "description",
        "isEnabled", "isRequired", "forms", "reevaluateOnEachRequest", "defaultValue", "onlyIf"
    ];

    static readonly string[] _bindProperties = ["type", "datatype", "binding", "replaces", "fileRename", "description", "isEnabled", "isRequired", "forms"];

    /// <summary>
    /// Parses the <c language="csharp">symbols</c> section into symbol configurations.
    /// </summary>
    /// <param name="root">The manifest root element.</param>
    /// <returns>Symbols keyed by name.</returns>
    public static IReadOnlyDictionary<string, SymbolConfig> ParseSymbols(JsonElement root)
    {
        var result = new Dictionary<string, SymbolConfig>();
        foreach (var (name, element) in Json.GetObjectProperties(root, "symbols"))
        {
            result[name] = ParseSymbol(name, element);
        }
        return result;
    }

    /// <summary>
    /// Parses a single symbol definition.
    /// </summary>
    /// <param name="name">The symbol name.</param>
    /// <param name="element">The symbol element.</param>
    /// <returns>The symbol configuration.</returns>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    public static SymbolConfig ParseSymbol(string name, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidTemplateManifest($"template.json: symbol '{name}' must be an object.");
        }

        var typeText = Json.GetString(element, "type")?.ToLowerInvariant()
            ?? throw new InvalidTemplateManifest($"template.json: symbol '{name}' is missing 'type'.");
        var known = typeText switch
        {
            "parameter" => _parameterProperties,
            "derived" => _derivedProperties,
            "computed" => _computedProperties,
            "generated" => _generatedProperties,
            "bind" => _bindProperties,
            _ => throw new InvalidTemplateManifest(
                $"template.json: symbol '{name}' has unknown type '{typeText}'. Known types: parameter, derived, computed, generated, bind.")
        };
        Json.RejectUnknownProperties(element, $"template.json: symbols.{name}", known);

        var type = typeText switch
        {
            "parameter" => SymbolType.Parameter,
            "derived" => SymbolType.Derived,
            "computed" => SymbolType.Computed,
            "generated" => SymbolType.Generated,
            _ => SymbolType.Bind
        };

        var generatorParameters = new Dictionary<string, JsonElement>();
        if (element.TryGetProperty("parameters", out var parametersElement)
            && parametersElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in parametersElement.EnumerateObject())
            {
                generatorParameters[property.Name] = property.Value.Clone();
            }
        }

        return new SymbolConfig
        {
            Name = name,
            Type = type,
            DataType = Json.GetString(element, "datatype") ?? Json.GetString(element, "dataType"),
            Replaces = Json.GetString(element, "replaces"),
            FileRename = Json.GetString(element, "fileRename"),
            Description = Json.GetString(element, "description"),
            DisplayName = Json.GetString(element, "displayName"),
            Prompt = ParsePrompt(element),
            DefaultValue = Json.GetString(element, "defaultValue"),
            DefaultIfOptionWithoutValue = Json.GetString(element, "defaultIfOptionWithoutValue"),
            AllowMultipleValues = Json.GetBool(element, "allowMultipleValues") ?? false,
            EnableQuotelessLiterals = Json.GetBool(element, "enableQuotelessLiterals") ?? false,
            Choices = ParseChoices(name, element),
            ValueSource = Json.GetString(element, "valueSource"),
            ValueTransform = ParseValueTransform(name, element),
            Value = Json.GetString(element, "value"),
            Evaluator = Json.GetString(element, "evaluator"),
            Generator = Json.GetString(element, "generator"),
            GeneratorParameters = generatorParameters,
            Binding = Json.GetString(element, "binding"),
            IsEnabled = Json.GetString(element, "isEnabled"),
            IsRequired = Json.GetString(element, "isRequired"),
            Forms = ParseForms(element)
        };
    }

    static string? ParsePrompt(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty("prompt", out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => null,
            _ => throw new InvalidTemplateManifest("template.json: 'prompt' must be a string or boolean.")
        };
    }

    static string[] ParseValueTransform(string name, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty("valueTransform", out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => [value.GetString()!],
            JsonValueKind.Array when value.EnumerateArray().All(item => item.ValueKind == JsonValueKind.String) =>
                [.. value.EnumerateArray().Select(item => item.GetString()!)],
            _ => throw new InvalidTemplateManifest($"template.json: symbols.{name}.valueTransform must be a string or array of strings.")
        };
    }

    static List<ChoiceConfig> ParseChoices(string name, JsonElement element)
    {
        var choices = new List<ChoiceConfig>();
        foreach (var choiceElement in Json.GetArray(element, "choices"))
        {
            if (choiceElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidTemplateManifest($"template.json: symbols.{name}.choices entries must be objects.");
            }
            Json.RejectUnknownProperties(choiceElement, $"template.json: symbols.{name}.choices", "choice", "description", "displayName");
            var choice = Json.GetString(choiceElement, "choice")
                ?? throw new InvalidTemplateManifest($"template.json: symbols.{name}.choices entry is missing 'choice'.");
            choices.Add(new ChoiceConfig
            {
                Choice = choice,
                Description = Json.GetString(choiceElement, "description")
            });
        }
        return choices;
    }

    static Dictionary<string, IReadOnlyList<string>> ParseForms(JsonElement element)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var (glob, formsElement) in Json.GetObjectProperties(element, "forms"))
        {
            if (glob == "global" || glob.Contains('*'))
            {
                result[glob] = [.. formsElement.EnumerateArray().Select(form => form.GetString() ?? string.Empty)];
                continue;
            }
            throw new UnsupportedTemplateConstruct(
                "template.json: symbols.forms",
                $"the forms key '{glob}' is neither 'global' nor a glob pattern.");
        }
        return result;
    }
}
