// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle;

namespace Cratis.Cli;

/// <summary>
/// Factory for creating a fully configured Cratis CLI <see cref="CommandApp"/>.
/// </summary>
public static partial class CliApp
{
    /// <summary>
    /// Creates a new <see cref="CommandApp"/> with all Cratis CLI commands registered.
    /// </summary>
    /// <returns>A configured <see cref="CommandApp"/> ready to run.</returns>
    public static CommandApp Create()
    {
        var app = new CommandApp();

        app.Configure(config =>
        {
            config.SetApplicationName("cratis");
            config.SetApplicationVersion(typeof(CliApp).Assembly.GetName().Version?.ToString() ?? "0.0.0");
            config.SetInterceptor(new EventStoreInterceptor());
            RegisterDiscoveredCommands(config);
        });

        return app;
    }
}
