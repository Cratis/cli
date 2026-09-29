// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRunner.when_resolving_the_target;

public class without_a_stored_login : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => DirectMcpRunner.Resolve(
        new DirectConfiguration { Origin = "https://direct.example", Tenant = "team", Issuer = "https://identity.example/" },
        new("https://elsewhere.example", null)));

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<DirectAuthError>();
    [Fact] void should_not_inherit_the_active_tenant_on_another_origin() => _error.Message.ShouldNotContain("team");
    [Fact] void should_tell_the_user_how_to_log_in() => _error.Message.ShouldContain("cratis direct login --url https://elsewhere.example");
}
