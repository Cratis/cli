// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.LlmContext;

/// <summary>
/// Describes a single CLI command within a group.
/// </summary>
/// <param name="Name">The command name (e.g. "list").</param>
/// <param name="Description">A description of the command.</param>
/// <param name="InheritedOptions">Options inherited from the parent group (e.g. event store settings). Null when the parent group already declares them.</param>
/// <param name="Arguments">Positional arguments (e.g. &lt;OBSERVER_ID&gt;) in order. Null when the command has no positional arguments.</param>
/// <param name="Options">Named flags and options (e.g. --type). Null when the command has no named options.</param>
public record CommandDescriptor(string Name, string Description, IReadOnlyList<OptionDescriptor>? InheritedOptions, IReadOnlyList<OptionDescriptor>? Arguments, IReadOnlyList<OptionDescriptor>? Options);
