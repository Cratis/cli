// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectLoginFlow;

public class when_using_another_tenant_after_logout : for_DirectCredentials.given.stored_credentials
{
    DirectTarget _target = null!;

    void Establish() => DirectCredentials.Forget(_config, _config.Credentials.Single(entry => entry.Origin == _config.Origin && entry.Tenant == _config.Tenant));
    void Because() => _target = DirectLoginFlow.TargetFor(new DirectUseSettings { Tenant = "previous" }, _config, "previous");

    [Fact] void should_reauthorize_on_the_previous_origin() => DirectCredentials.OriginOf(_target).ShouldEqual("https://direct.example");
    [Fact] void should_use_the_requested_tenant() => _target.Tenant.ShouldEqual("previous");
    [Fact] void should_address_the_remaining_stored_credential() => DirectCredentials.Find(_config, _target).ShouldNotBeNull();
}
