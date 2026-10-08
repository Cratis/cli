// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectMcpCommand.given;

public class an_inactive_login : Cli.given.a_temp_config_directory
{
    protected DirectConfiguration _direct;

    async Task Establish()
    {
        _direct = new DirectConfiguration
        {
            Origin = "https://direct.example",
            Tenant = "active",
            Issuer = "https://identity.example/",
            Credentials =
            [
                new() { Origin = "https://direct.example", Tenant = "active", Issuer = "https://identity.example/" },
                new() { Origin = "https://direct.example", Issuer = "https://identity.example/" }
            ]
        };
        using var server = new a_fake_authorization_server();
        foreach (var entry in _direct.Credentials) server.Store(DirectTarget.Create(entry.Origin, entry.Tenant), $"refresh-{entry.Tenant ?? "none"}");
        await DirectCredentials.Logout(_direct, DirectCredentials.Select(_direct, null, null, false), _ => server.Provider(), CancellationToken.None);
        new CliConfiguration { Direct = _direct }.Save();
    }
}
