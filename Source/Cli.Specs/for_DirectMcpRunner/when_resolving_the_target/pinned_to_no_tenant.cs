// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRunner.when_resolving_the_target;

public class pinned_to_no_tenant : Specification
{
    DirectConfiguration _config;
    DirectTarget _target;

    void Establish()
    {
        _config = new DirectConfiguration { Origin = "https://direct.example", Tenant = "team", Issuer = "https://identity.example/" };
        _config.Credentials.Add(new DirectCredentialEntry { Origin = "https://direct.example", Tenant = "team", Issuer = "https://identity.example/" });
        _config.Credentials.Add(new DirectCredentialEntry { Origin = "https://direct.example", Issuer = "https://tenantless.example/" });
    }

    void Because() => (_target, _) = DirectMcpRunner.Resolve(_config, new(null, null, NoTenant: true));

    [Fact] void should_use_the_active_origin() => _target.Resource.ShouldEqual(new Uri("https://direct.example/mcp"));
    [Fact] void should_not_inherit_the_active_tenant() => _target.Tenant.ShouldBeNull();
}
