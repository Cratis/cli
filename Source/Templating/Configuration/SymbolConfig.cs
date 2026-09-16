// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

namespace Cratis.Templating.Configuration;

/// <summary>
/// Represents the type of a symbol defined in a template manifest.
/// </summary>
public enum SymbolType
{
    /// <summary>The symbol's value is provided by the user as a parameter.</summary>
    Parameter,

    /// <summary>The symbol's value is derived from another symbol through value forms.</summary>
    Derived,

    /// <summary>The symbol's value is computed from a boolean expression.</summary>
    Computed,

    /// <summary>The symbol's value is produced by a generator.</summary>
    Generated,

    /// <summary>The symbol's value is bound from host-provided data.</summary>
    Bind
}

/// <summary>
/// Represents a symbol definition from a template manifest, covering all five symbol types.
/// </summary>
public record SymbolConfig
{
    /// <summary>Gets the symbol name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the symbol type.</summary>
    public required SymbolType Type { get; init; }

    /// <summary>Gets the data type: bool, choice, float, int, string, hex or text.</summary>
    public string? DataType { get; init; }

    /// <summary>Gets the content replacement token, when set.</summary>
    public string? Replaces { get; init; }

    /// <summary>Gets the file rename token, when set.</summary>
    public string? FileRename { get; init; }

    /// <summary>Gets the description, localized when a localization overlay is present.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the display name shown in help output.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Gets the prompt text shown when interactively collecting the value.</summary>
    public string? Prompt { get; init; }

    /// <summary>Gets the default value applied when the user provides none.</summary>
    public string? DefaultValue { get; init; }

    /// <summary>Gets the value applied when the flag is passed without a value.</summary>
    public string? DefaultIfOptionWithoutValue { get; init; }

    /// <summary>Gets a value indicating whether the value may combine multiple choices.</summary>
    public bool AllowMultipleValues { get; init; }

    /// <summary>Gets a value indicating whether quoteless choice literals are permitted in expressions.</summary>
    public bool EnableQuotelessLiterals { get; init; }

    /// <summary>Gets the available choices for a choice parameter.</summary>
    public IReadOnlyList<ChoiceConfig> Choices { get; init; } = [];

    /// <summary>Gets the source variable for derived symbols.</summary>
    public string? ValueSource { get; init; }

    /// <summary>Gets the value form(s) applied for derived symbols.</summary>
    public IReadOnlyList<string> ValueTransform { get; init; } = [];

    /// <summary>Gets the boolean expression for computed symbols.</summary>
    public string? Value { get; init; }

    /// <summary>Gets the expression evaluator for computed symbols.</summary>
    public string? Evaluator { get; init; }

    /// <summary>Gets the generator name for generated symbols.</summary>
    public string? Generator { get; init; }

    /// <summary>Gets the raw generator parameters, consumed by the generator implementations.</summary>
    public IReadOnlyDictionary<string, JsonElement> GeneratorParameters { get; init; } =
        new Dictionary<string, JsonElement>();

    /// <summary>Gets the binding path for bind symbols.</summary>
    public string? Binding { get; init; }

    /// <summary>Gets the condition controlling whether the symbol is enabled.</summary>
    public string? IsEnabled { get; init; }

    /// <summary>Gets the condition controlling whether the symbol is required.</summary>
    public string? IsRequired { get; init; }

    /// <summary>Gets the value-form references per glob, keyed by glob with the special key global.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Forms { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();
}

/// <summary>
/// Represents one choice of a choice parameter.
/// </summary>
public record ChoiceConfig
{
    /// <summary>Gets the choice value.</summary>
    public required string Choice { get; init; }

    /// <summary>Gets the localized description of the choice.</summary>
    public string? Description { get; init; }
}
