// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.LlmContext;

/// <summary>
/// Describes a command group (branch) in the CLI, preserving the full branch hierarchy.
/// </summary>
/// <param name="Name">The branch name (e.g. "chronicle", "observers").</param>
/// <param name="Description">A description of the command group.</param>
/// <param name="InheritedOptions">Options shared by all commands in this group. When set, individual commands omit them to avoid repetition.</param>
/// <param name="Commands">The leaf commands directly within this group.</param>
/// <param name="SubGroups">Nested command groups (sub-branches) within this group.</param>
public record CommandGroupDescriptor(
    string Name,
    string Description,
    IReadOnlyList<OptionDescriptor>? InheritedOptions,
    IReadOnlyList<CommandDescriptor>? Commands,
    IReadOnlyList<CommandGroupDescriptor>? SubGroups);
