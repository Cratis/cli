// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle;
using Cratis.Cli.Commands.Version;

namespace Cratis.Cli;

/// <summary>
/// Factory for creating a fully configured Cratis CLI <see cref="CommandApp"/>.
/// </summary>
public static partial class CliApp
{
    /// <summary>
    /// Creates a new <see cref="CommandApp"/> with all Cratis CLI commands registered.
    /// </summary>
    /// <param name="console">The console for framework help and diagnostics, or the default console.</param>
    /// <returns>A configured <see cref="CommandApp"/> ready to run.</returns>
    public static CommandApp Create(IAnsiConsole? console = null)
    {
        var app = new CommandApp();

        app.Configure(config =>
        {
            if (console is not null) config.ConfigureConsole(console);
            config.SetApplicationName("cratis");
            config.SetApplicationVersion(VersionCommand.GetCliVersion());
            config.SetInterceptor(new EventStoreInterceptor());
            RegisterDiscoveredCommands(config);
        });

        return app;
    }
}
