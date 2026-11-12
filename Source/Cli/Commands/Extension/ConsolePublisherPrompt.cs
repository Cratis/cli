// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions;

namespace Cratis.Cli.Commands.Extension;

/// <summary>
/// Asks on the console whether to trust a publisher seen for the first time.
/// </summary>
public class ConsolePublisherPrompt : IPublisherPrompt
{
    /// <inheritdoc/>
    public bool IsInteractive => GlobalSettings.IsInteractiveEnvironment();

    /// <inheritdoc/>
    public bool Confirm(string publisher, string? fingerprint, string resolved) =>
        AnsiConsole.Confirm($"Trust publisher '{Markup.Escape(publisher)}'{(fingerprint is null ? string.Empty : $" (key {fingerprint})")} to supply '{Markup.Escape(resolved)}'? It is recorded in .cratis/{ExtensionAllowList.FileName}.", false);
}
