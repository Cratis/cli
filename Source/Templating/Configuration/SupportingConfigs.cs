// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

namespace Cratis.Templating.Configuration;

/// <summary>
/// Represents a rename rule: a file whose path matches <see cref="Pattern"/> is renamed to <see cref="Replacement"/>.
/// </summary>
public record RenameConfig
{
    /// <summary>Gets the pattern to match in the path.</summary>
    public required string Pattern { get; init; }

    /// <summary>Gets the replacement string, which may reference symbol values.</summary>
    public required string Replacement { get; init; }
}

/// <summary>
/// Represents a source modifier with its own include/exclude/copyOnly/rename/condition rules.
/// </summary>
public record SourceModifierConfig
{
    /// <summary>Gets the condition controlling whether the modifier applies.</summary>
    public string? Condition { get; init; }

    /// <summary>Gets the include globs.</summary>
    public IReadOnlyList<string> Include { get; init; } = [];

    /// <summary>Gets the exclude globs.</summary>
    public IReadOnlyList<string> Exclude { get; init; } = [];

    /// <summary>Gets the copy-only globs — matched files are copied byte-exact without processing.</summary>
    public IReadOnlyList<string> CopyOnly { get; init; } = [];

    /// <summary>Gets the rename rules.</summary>
    public IReadOnlyList<RenameConfig> Rename { get; init; } = [];
}

/// <summary>
/// Represents one entry of the <c language="csharp">sources</c> array in a template manifest.
/// </summary>
public record SourceConfig
{
    /// <summary>Gets the source folder inside the template, relative to the template root. Defaults to the root.</summary>
    public string Source { get; init; } = "./";

    /// <summary>Gets the target folder inside the output, relative to the output root. Defaults to the root.</summary>
    public string Target { get; init; } = "./";

    /// <summary>Gets the include globs.</summary>
    public IReadOnlyList<string> Include { get; init; } = [];

    /// <summary>Gets the exclude globs.</summary>
    public IReadOnlyList<string> Exclude { get; init; } = [];

    /// <summary>Gets the copy-only globs — matched files are copied byte-exact without processing.</summary>
    public IReadOnlyList<string> CopyOnly { get; init; } = [];

    /// <summary>Gets the rename rules.</summary>
    public IReadOnlyList<RenameConfig> Rename { get; init; } = [];

    /// <summary>Gets the condition controlling whether the source applies.</summary>
    public string? Condition { get; init; }

    /// <summary>Gets the modifiers.</summary>
    public IReadOnlyList<SourceModifierConfig> Modifiers { get; init; } = [];
}

/// <summary>
/// Represents a manual instruction attached to a post action.
/// </summary>
public record ManualInstructionConfig
{
    /// <summary>Gets the instruction text.</summary>
    public required string Text { get; init; }

    /// <summary>Gets the condition controlling whether this instruction applies.</summary>
    public string? Condition { get; init; }
}

/// <summary>
/// Represents a post action from a template manifest.
/// </summary>
public record PostActionConfig
{
    /// <summary>Gets the action identifier, matching the documented well-known action ids.</summary>
    public required string ActionId { get; init; }

    /// <summary>Gets the localized description of the action.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the condition controlling whether the action runs.</summary>
    public string? Condition { get; init; }

    /// <summary>Gets a value indicating whether a failure of this action continues the run.</summary>
    public bool ContinueOnError { get; init; }

    /// <summary>Gets the action arguments, keyed by argument name.</summary>
    public IReadOnlyDictionary<string, string> Args { get; init; } = new Dictionary<string, string>();

    /// <summary>Gets the manual instructions to show when the action cannot run or fails.</summary>
    public IReadOnlyList<ManualInstructionConfig> ManualInstructions { get; init; } = [];
}

/// <summary>
/// Represents a constraint on template usage, such as operating system or host.
/// </summary>
public record ConstraintConfig
{
    /// <summary>Gets the constraint type: os, host, sdk-version, workload or project-capability.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the allowed values for the constraint.</summary>
    public IReadOnlyList<string> Allowed { get; init; } = [];
}

/// <summary>
/// Represents a baseline — an alternate set of symbol defaults selected by name.
/// </summary>
public record BaselineConfig
{
    /// <summary>Gets the baseline name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the baseline description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the symbol values the baseline applies as defaults.</summary>
    public IReadOnlyDictionary<string, string> Symbols { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents a user-defined value form from the top-level <c language="csharp">forms</c> section.
/// </summary>
public record ValueFormConfig
{
    /// <summary>Gets the form identifier, such as <c language="csharp">kebabCase</c> or <c language="csharp">replace</c>.</summary>
    public required string Identifier { get; init; }

    /// <summary>Gets the regex pattern for <c language="csharp">replace</c> forms.</summary>
    public string? Pattern { get; init; }

    /// <summary>Gets the regex replacement for <c language="csharp">replace</c> forms.</summary>
    public string? Replacement { get; init; }

    /// <summary>Gets the chain steps for <c language="csharp">chain</c> forms.</summary>
    public IReadOnlyList<string> Steps { get; init; } = [];
}
