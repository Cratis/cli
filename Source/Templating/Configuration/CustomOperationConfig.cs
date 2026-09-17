// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

namespace Cratis.Templating.Configuration;

/// <summary>
/// Represents the type of a custom operation.
/// </summary>
public enum CustomOperationType
{
    /// <summary>Conditional directives with custom tokens.</summary>
    Conditional,

    /// <summary>Direct token replacement.</summary>
    Replacement,

    /// <summary>Sets a flag symbol when a token is encountered.</summary>
    Flag,

    /// <summary>Includes another file at a token position.</summary>
    Include,

    /// <summary>Region directives with custom begin/end markers.</summary>
    Region,

    /// <summary>Expands symbol variables in a custom prefix/suffix form.</summary>
    ExpandVariables,

    /// <summary>Balanced nesting markers that protect content from processing.</summary>
    BalancedNesting
}

/// <summary>
/// Represents a custom operation from <c language="csharp">globalCustomOperations</c> or <c language="csharp">specialCustomOperations</c>.
/// </summary>
public record CustomOperationConfig
{
    /// <summary>Gets the operation type.</summary>
    public required CustomOperationType Type { get; init; }

    /// <summary>Gets the condition controlling whether the operation applies.</summary>
    public string? Condition { get; init; }

    /// <summary>Gets the directive tokens, keyed by directive role (if, else, elseif, endif and their actionable variants).</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Tokens { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Gets the uncomment actions applied to actionable directive bodies: cStyleUncomment, cStyleReduceComment.</summary>
    public IReadOnlyList<string> Actions { get; init; } = [];

    /// <summary>Gets a value indicating whether directive comment tokens are trimmed from emitted lines.</summary>
    public bool? Trim { get; init; }

    /// <summary>Gets a value indicating whether directives must occupy a whole line.</summary>
    public bool? WholeLine { get; init; }

    /// <summary>Gets the expression evaluator name.</summary>
    public string? Evaluator { get; init; }

    /// <summary>Gets the target variable for replacement operations.</summary>
    public string? Variable { get; init; }

    /// <summary>Gets the replacement value for replacement operations.</summary>
    public string? Replacement { get; init; }

    /// <summary>Gets the token for replacement, flag and include operations.</summary>
    public string? Token { get; init; }

    /// <summary>Gets the include path for include operations.</summary>
    public string? IncludePath { get; init; }

    /// <summary>Gets the begin marker for region and balanced nesting operations.</summary>
    public string? Begin { get; init; }

    /// <summary>Gets the end marker for region and balanced nesting operations.</summary>
    public string? End { get; init; }

    /// <summary>Gets the region name for region operations.</summary>
    public string? RegionName { get; init; }

    /// <summary>Gets the variable prefix for expandVariables operations.</summary>
    public string? Prefix { get; init; }

    /// <summary>Gets the variable suffix for expandVariables operations.</summary>
    public string? Suffix { get; init; }

    /// <summary>Gets the glob this operation is scoped to; null for global operations.</summary>
    public string? Glob { get; init; }
}
