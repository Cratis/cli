// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console.Cli.Help;
using Spectre.Console.Rendering;

namespace Cratis.Cli.Commands.Direct;

/// <summary>Includes the bridge's default-command options in the MCP branch's help without inheriting them into registration commands.</summary>
/// <param name="settings">The CLI's help settings.</param>
internal sealed class DirectMcpHelp(ICommandAppSettings settings) : HelpProvider(settings)
{
    /// <inheritdoc/>
    public override IEnumerable<IRenderable> GetOptions(ICommandModel model, ICommandInfo? command) =>
        base.GetOptions(model, string.Equals(command?.Name, "mcp", StringComparison.Ordinal) && string.Equals(command?.Parent?.Name, "direct", StringComparison.Ordinal) && command?.DefaultCommand is { } bridge ? bridge : command);
}
