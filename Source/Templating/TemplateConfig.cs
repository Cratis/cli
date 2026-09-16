// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

using Cratis.Templating.Configuration;

namespace Cratis.Templating;

/// <summary>
/// Represents a parsed <c language="csharp">template.json</c> manifest, covering every documented top-level property.
/// Unknown properties, malformed values and unsupported constructs fail with named errors — nothing is
/// silently ignored.
/// </summary>
public record TemplateConfig
{
    /// <summary>
    /// All top-level properties the engine knows, including the two the published schema omits.
    /// </summary>
    public static readonly string[] KnownTopLevelProperties =
    [
        "author", "baselines", "classifications", "constraints", "defaultName", "description",
        "forms", "generatorVersions", "globalCustomOperations", "groupIdentity", "guids", "identity",
        "name", "placeholderFilename", "postActions", "precedence", "preferDefaultName",
        "preferNameDirectory", "primaryOutputs", "shortName", "sourceName", "sources", "specialCustomOperations",
        "symbols", "tags", "thirdPartyNotices"
    ];

    /// <summary>Gets the template author.</summary>
    public string? Author { get; init; }

    /// <summary>Gets the template name shown to users.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the short name used for instantiation.</summary>
    public required string ShortName { get; init; }

    /// <summary>Gets the stable template identity.</summary>
    public string? Identity { get; init; }

    /// <summary>Gets the description shown in listings.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the template classifications.</summary>
    public IReadOnlyList<string> Classifications { get; init; } = [];

    /// <summary>Gets the tags, keyed by tag name.</summary>
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();

    /// <summary>Gets the group identity used to collapse template families in listings.</summary>
    public string? GroupIdentity { get; init; }

    /// <summary>Gets the precedence within the group identity.</summary>
    public int Precedence { get; init; }

    /// <summary>Gets the default name for instantiations.</summary>
    public string? DefaultName { get; init; }

    /// <summary>Gets a value indicating whether the default name is always used and the name cannot be changed.</summary>
    public bool PreferDefaultName { get; init; }

    /// <summary>Gets a value indicating whether the output is placed in a directory named after the instantiated name.</summary>
    public bool PreferNameDirectory { get; init; }

    /// <summary>Gets the placeholder filename marker for empty folders and dot-prefixed files.</summary>
    public string? PlaceholderFilename { get; init; }

    /// <summary>Gets the source name replaced in file contents and paths.</summary>
    public string? SourceName { get; init; }

    /// <summary>Gets the GUID values found in template sources that must be replaced with generated GUIDs.</summary>
    public IReadOnlyList<string> Guids { get; init; } = [];

    /// <summary>Gets the symbols, keyed by symbol name.</summary>
    public IReadOnlyDictionary<string, SymbolConfig> Symbols { get; init; } = new Dictionary<string, SymbolConfig>();

    /// <summary>Gets the sources.</summary>
    public IReadOnlyList<SourceConfig> Sources { get; init; } = [];

    /// <summary>Gets the post actions.</summary>
    public IReadOnlyList<PostActionConfig> PostActions { get; init; } = [];

    /// <summary>Gets the primary outputs.</summary>
    public IReadOnlyList<PrimaryOutputConfig> PrimaryOutputs { get; init; } = [];

    /// <summary>Gets the constraints, keyed by constraint name.</summary>
    public IReadOnlyDictionary<string, ConstraintConfig> Constraints { get; init; } = new Dictionary<string, ConstraintConfig>();

    /// <summary>Gets the baselines, keyed by baseline name.</summary>
    public IReadOnlyDictionary<string, BaselineConfig> Baselines { get; init; } = new Dictionary<string, BaselineConfig>();

    /// <summary>Gets the user-defined value forms, keyed by form name.</summary>
    public IReadOnlyDictionary<string, ValueFormConfig> Forms { get; init; } = new Dictionary<string, ValueFormConfig>();

    /// <summary>Gets the global custom operations.</summary>
    public IReadOnlyList<CustomOperationConfig> GlobalCustomOperations { get; init; } = [];

    /// <summary>Gets the special (glob-scoped) custom operations, keyed by glob.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<CustomOperationConfig>> SpecialCustomOperations { get; init; } =
        new Dictionary<string, IReadOnlyList<CustomOperationConfig>>();

    /// <summary>Gets the third-party notices path.</summary>
    public string? ThirdPartyNotices { get; init; }

    /// <summary>Gets the generator versions, keyed by generator name.</summary>
    public IReadOnlyDictionary<string, string> GeneratorVersions { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets the choice values of parameters that opted into quoteless literals — identifiers that
    /// evaluate as string literals in expressions rather than unresolved symbols.
    /// </summary>
    public IReadOnlyCollection<string> QuotelessChoiceLiterals =>
    [
        .. Symbols.Values
            .Where(symbol => symbol.Type == SymbolType.Parameter && symbol.EnableQuotelessLiterals)
            .SelectMany(symbol => symbol.Choices.Select(choice => choice.Choice))
    ];
}

/// <summary>
/// Represents a primary output — a file produced by instantiation, usually consumed by post actions.
/// </summary>
public record PrimaryOutputConfig
{
    /// <summary>Gets the relative path to the file after instantiation.</summary>
    public required string Path { get; init; }

    /// <summary>Gets the condition controlling whether the output applies.</summary>
    public string? Condition { get; init; }
}
