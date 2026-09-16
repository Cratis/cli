// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

using Cratis.Templating.Configuration;

namespace Cratis.Templating;

/// <summary>
/// Parses sources, post actions, primary outputs, constraints, baselines and user-defined value forms.
/// </summary>
static class ManifestSectionParsing
{
    /// <summary>
    /// Parses the <c language="csharp">sources</c> section.
    /// </summary>
    /// <param name="root">The manifest root element.</param>
    /// <returns>The source configurations.</returns>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    public static IReadOnlyList<SourceConfig> ParseSources(JsonElement root)
    {
        var sources = new List<SourceConfig>();
        foreach (var sourceElement in Json.GetArray(root, "sources"))
        {
            if (sourceElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidTemplateManifest("template.json: sources entries must be objects.");
            }
            Json.RejectUnknownProperties(
                sourceElement,
                "template.json: sources",
                "source",
                "target",
                "include",
                "exclude",
                "copyOnly",
                "rename",
                "condition",
                "modifiers");

            var modifiers = new List<SourceModifierConfig>();
            foreach (var modifierElement in Json.GetArray(sourceElement, "modifiers"))
            {
                Json.RejectUnknownProperties(
                    modifierElement,
                    "template.json: sources.modifiers",
                    "include",
                    "exclude",
                    "copyOnly",
                    "rename",
                    "condition");
                modifiers.Add(new SourceModifierConfig
                {
                    Condition = Json.GetString(modifierElement, "condition"),
                    Include = Json.GetStringArray(modifierElement, "include"),
                    Exclude = Json.GetStringArray(modifierElement, "exclude"),
                    CopyOnly = Json.GetStringArray(modifierElement, "copyOnly"),
                    Rename = ParseRenames(modifierElement)
                });
            }

            sources.Add(new SourceConfig
            {
                Source = Json.GetString(sourceElement, "source") ?? "./",
                Target = Json.GetString(sourceElement, "target") ?? "./",
                Include = Json.GetStringArray(sourceElement, "include"),
                Exclude = Json.GetStringArray(sourceElement, "exclude"),
                CopyOnly = Json.GetStringArray(sourceElement, "copyOnly"),
                Rename = ParseRenames(sourceElement),
                Condition = Json.GetString(sourceElement, "condition"),
                Modifiers = modifiers
            });
        }
        return sources;
    }

    /// <summary>
    /// Parses the <c language="csharp">postActions</c> section.
    /// </summary>
    /// <param name="root">The manifest root element.</param>
    /// <returns>The post action configurations.</returns>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    public static IReadOnlyList<PostActionConfig> ParsePostActions(JsonElement root)
    {
        var actions = new List<PostActionConfig>();
        foreach (var actionElement in Json.GetArray(root, "postActions"))
        {
            if (actionElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidTemplateManifest("template.json: postActions entries must be objects.");
            }
            Json.RejectUnknownProperties(
                actionElement,
                "template.json: postActions",
                "actionId",
                "description",
                "condition",
                "continueOnError",
                "args",
                "manualInstructions",
                "configFile");

            var actionId = Json.GetString(actionElement, "actionId")
                ?? throw new InvalidTemplateManifest("template.json: postActions entry is missing 'actionId'.");

            var args = new Dictionary<string, string>();
            foreach (var (argName, argValue) in Json.GetObjectProperties(actionElement, "args"))
            {
                args[argName] = argValue.ValueKind switch
                {
                    JsonValueKind.String => argValue.GetString()!,
                    JsonValueKind.Number => argValue.GetRawText(),
                    JsonValueKind.True or JsonValueKind.False => argValue.GetRawText(),
                    _ => throw new InvalidTemplateManifest($"template.json: postActions.args.{argName} must be a scalar value.")
                };
            }

            var instructions = new List<ManualInstructionConfig>();
            foreach (var instructionElement in Json.GetArray(actionElement, "manualInstructions"))
            {
                Json.RejectUnknownProperties(instructionElement, "template.json: postActions.manualInstructions", "text", "condition");
                instructions.Add(new ManualInstructionConfig
                {
                    Text = Json.GetString(instructionElement, "text") ?? string.Empty,
                    Condition = Json.GetString(instructionElement, "condition")
                });
            }

            actions.Add(new PostActionConfig
            {
                ActionId = actionId,
                Description = Json.GetString(actionElement, "description"),
                Condition = Json.GetString(actionElement, "condition"),
                ContinueOnError = Json.GetBool(actionElement, "continueOnError") ?? false,
                Args = args,
                ManualInstructions = instructions
            });
        }
        return actions;
    }

    /// <summary>
    /// Parses the <c language="csharp">primaryOutputs</c> section.
    /// </summary>
    /// <param name="root">The manifest root element.</param>
    /// <returns>The primary output configurations.</returns>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    public static IReadOnlyList<PrimaryOutputConfig> ParsePrimaryOutputs(JsonElement root)
    {
        var outputs = new List<PrimaryOutputConfig>();
        foreach (var outputElement in Json.GetArray(root, "primaryOutputs"))
        {
            if (outputElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidTemplateManifest("template.json: primaryOutputs entries must be objects.");
            }
            Json.RejectUnknownProperties(outputElement, "template.json: primaryOutputs", "path", "condition");
            outputs.Add(new PrimaryOutputConfig
            {
                Path = Json.GetString(outputElement, "path")
                    ?? throw new InvalidTemplateManifest("template.json: primaryOutputs entry is missing 'path'."),
                Condition = Json.GetString(outputElement, "condition")
            });
        }
        return outputs;
    }

    /// <summary>
    /// Parses the <c language="csharp">constraints</c> section.
    /// </summary>
    /// <param name="root">The manifest root element.</param>
    /// <returns>Constraints keyed by name.</returns>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    public static IReadOnlyDictionary<string, ConstraintConfig> ParseConstraints(JsonElement root)
    {
        var result = new Dictionary<string, ConstraintConfig>();
        foreach (var (name, element) in Json.GetObjectProperties(root, "constraints"))
        {
            Json.RejectUnknownProperties(element, $"template.json: constraints.{name}", "type", "args");
            var type = Json.GetString(element, "type")
                ?? throw new InvalidTemplateManifest($"template.json: constraints.{name} is missing 'type'.");
            var allowed = new List<string>();
            if (element.TryGetProperty("args", out var argsElement) && argsElement.ValueKind != JsonValueKind.Null)
            {
                if (argsElement.ValueKind == JsonValueKind.String)
                {
                    allowed.Add(argsElement.GetString()!);
                }
                else if (argsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var arg in argsElement.EnumerateArray())
                    {
                        CollectConstraintValues(arg, allowed);
                    }
                }
                else
                {
                    CollectConstraintValues(argsElement, allowed);
                }
            }

            result[name] = new ConstraintConfig { Type = type.ToLowerInvariant(), Allowed = allowed };
        }
        return result;
    }

    /// <summary>
    /// Parses the <c language="csharp">baselines</c> section.
    /// </summary>
    /// <param name="root">The manifest root element.</param>
    /// <returns>Baselines keyed by name.</returns>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    public static IReadOnlyDictionary<string, BaselineConfig> ParseBaselines(JsonElement root)
    {
        var result = new Dictionary<string, BaselineConfig>();
        foreach (var (name, element) in Json.GetObjectProperties(root, "baselines"))
        {
            Json.RejectUnknownProperties(element, $"template.json: baselines.{name}", "description", "symbols");
            var symbols = new Dictionary<string, string>();
            foreach (var (symbolName, symbolElement) in Json.GetObjectProperties(element, "symbols"))
            {
                symbols[symbolName] = symbolElement.ValueKind == JsonValueKind.String
                    ? symbolElement.GetString()!
                    : throw new InvalidTemplateManifest($"template.json: baselines.{name}.symbols.{symbolName} must be a string.");
            }
            result[name] = new BaselineConfig
            {
                Name = name,
                Description = Json.GetString(element, "description"),
                Symbols = symbols
            };
        }
        return result;
    }

    /// <summary>
    /// Parses the user-defined <c language="csharp">forms</c> section.
    /// </summary>
    /// <param name="root">The manifest root element.</param>
    /// <returns>Forms keyed by name.</returns>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    public static IReadOnlyDictionary<string, ValueFormConfig> ParseForms(JsonElement root)
    {
        var result = new Dictionary<string, ValueFormConfig>();
        foreach (var (name, element) in Json.GetObjectProperties(root, "forms"))
        {
            Json.RejectUnknownProperties(
                element,
                $"template.json: forms.{name}",
                "identifier",
                "pattern",
                "replacement",
                "steps");
            result[name] = new ValueFormConfig
            {
                Identifier = Json.GetString(element, "identifier")
                    ?? throw new InvalidTemplateManifest($"template.json: forms.{name} is missing 'identifier'."),
                Pattern = Json.GetString(element, "pattern"),
                Replacement = Json.GetString(element, "replacement"),
                Steps = Json.GetStringArray(element, "steps")
            };
        }
        return result;
    }

    static List<RenameConfig> ParseRenames(JsonElement element)
    {
        var renames = new List<RenameConfig>();
        foreach (var renameElement in Json.GetArray(element, "rename"))
        {
            Json.RejectUnknownProperties(renameElement, "template.json: rename", "pattern", "replacement");
            renames.Add(new RenameConfig
            {
                Pattern = Json.GetString(renameElement, "pattern")
                    ?? throw new InvalidTemplateManifest("template.json: rename entry is missing 'pattern'."),
                Replacement = Json.GetString(renameElement, "replacement")
                    ?? throw new InvalidTemplateManifest("template.json: rename entry is missing 'replacement'.")
            });
        }
        return renames;
    }

    static void CollectConstraintValues(JsonElement element, List<string> values)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            values.Add(element.GetString()!);
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name == "hostname")
                {
                    values.Add(property.Value.GetString()!);
                }
            }
        }
    }
}
