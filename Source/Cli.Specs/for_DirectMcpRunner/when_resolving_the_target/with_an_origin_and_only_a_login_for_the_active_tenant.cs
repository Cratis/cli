// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRunner.when_resolving_the_target;

public class with_an_origin_and_only_a_login_for_the_active_tenant : Specification
{
    DirectConfiguration _config;
    Exception _error;

    void Establish()
    {
        _config = new DirectConfiguration { Origin = "https://direct.example", Tenant = "team", Issuer = "https://identity.example/" };
        _config.Credentials.Add(new DirectCredentialEntry { Origin = "https://direct.example", Tenant = "team", Issuer = "https://identity.example/" });
    }

    void Because() => _error = Catch.Exception(() => DirectMcpRunner.Resolve(_config, new("https://direct.example", null)));

    [Fact] void should_refuse_rather_than_use_the_active_tenant() => _error.ShouldBeOfExactType<DirectAuthError>();
    [Fact] void should_not_name_the_active_tenant() => _error.Message.ShouldNotContain("team");
}
