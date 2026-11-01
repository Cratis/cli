// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRunner.when_resolving_the_target;

/// <summary>A registration written before any tenant was selected, started after 'cratis direct use team'.</summary>
public class with_an_origin_and_no_tenant : Specification
{
    DirectConfiguration _config;
    DirectTarget _target;
    DirectCredentialEntry _credential;

    void Establish()
    {
        _config = new DirectConfiguration { Origin = "https://direct.example", Tenant = "team", Issuer = "https://identity.example/" };
        _config.Credentials.Add(new DirectCredentialEntry { Origin = "https://direct.example", Tenant = "team", Issuer = "https://identity.example/" });
        _config.Credentials.Add(new DirectCredentialEntry { Origin = "https://direct.example", Issuer = "https://tenantless.example/" });
    }

    void Because() => (_target, _credential) = DirectMcpRunner.Resolve(_config, new("https://direct.example", null));

    [Fact] void should_not_inherit_the_active_tenant() => _target.Tenant.ShouldBeNull();
    [Fact] void should_pin_the_tenantless_credential() => _credential.Issuer.ShouldEqual("https://tenantless.example/");
}
