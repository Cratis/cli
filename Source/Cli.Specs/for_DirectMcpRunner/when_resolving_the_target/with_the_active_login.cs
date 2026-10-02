// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRunner.when_resolving_the_target;

public class with_the_active_login : Specification
{
    DirectConfiguration _config;
    DirectTarget _target;
    DirectCredentialEntry _credential;

    void Establish()
    {
        _config = new DirectConfiguration { Origin = "https://direct.example", Tenant = "team", Issuer = "https://identity.example/" };
        _config.Credentials.Add(new DirectCredentialEntry { Origin = "https://direct.example", Tenant = "other", Issuer = "https://other.example/" });
        _config.Credentials.Add(new DirectCredentialEntry { Origin = "https://direct.example", Tenant = "team", Issuer = "https://identity.example/", InsecureFileStore = true });
    }

    void Because() => (_target, _credential) = DirectMcpRunner.Resolve(_config, new(null, null));

    [Fact] void should_pin_the_active_origin() => _target.Resource.ShouldEqual(new Uri("https://direct.example/mcp"));
    [Fact] void should_pin_the_active_tenant() => _target.Tenant.ShouldEqual("team");
    [Fact] void should_pin_that_tenants_credential() => _credential.InsecureFileStore.ShouldBeTrue();
}
