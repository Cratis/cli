// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Completions;

/// <summary>
/// Represents a node in the CLI command tree used for generating shell completions.
/// </summary>
/// <param name="Name">The command or subcommand name.</param>
/// <param name="Description">A short description for completion hints.</param>
/// <param name="Options">Command-specific options (e.g. "--force").</param>
/// <param name="Children">Subcommands of this node.</param>
public record CommandNode(string Name, string Description, IReadOnlyList<string> Options, IReadOnlyList<CommandNode> Children)
{
    /// <summary>
    /// Creates a leaf command with no children.
    /// </summary>
    /// <param name="name">The command or subcommand name.</param>
    /// <param name="description">A short description for completion hints.</param>
    /// <param name="options">Command-specific options (e.g. "--force").</param>
    public CommandNode(string name, string description, IReadOnlyList<string> options)
        : this(name, description, options, [])
    {
    }

    /// <summary>
    /// Creates a leaf command with no options or children.
    /// </summary>
    /// <param name="name">The command or subcommand name.</param>
    /// <param name="description">A short description for completion hints.</param>
    public CommandNode(string name, string description)
        : this(name, description, [], [])
    {
    }

    /// <summary>
    /// Gets or sets the context used to dynamically complete the first positional argument via <c>cratis _complete &lt;context&gt;</c>.
    /// When <see langword="null"/>, no dynamic completion is applied.
    /// </summary>
    public string? DynamicCompletionContext { get; init; }
}
