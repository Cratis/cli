// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectCredentials;

public class when_forgetting_the_active_plaintext_credential : given.stored_credentials
{
    void Establish() => _config.InsecureFileStore = true;
    void Because() => DirectCredentials.Forget(_config, _config.Credentials.Single(entry => entry.Origin == _config.Origin && entry.Tenant == _config.Tenant));

    [Fact] void should_reset_the_active_origin() => _config.Origin.ShouldEqual("https://cratis.direct");
    [Fact] void should_clear_the_active_tenant() => _config.Tenant.ShouldBeNull();
    [Fact] void should_clear_the_active_issuer() => _config.Issuer.ShouldBeNull();
    [Fact] void should_clear_plaintext_consent() => _config.InsecureFileStore.ShouldBeFalse();
    [Fact] void should_preserve_other_credentials() => _config.Credentials.Count.ShouldEqual(3);
    [Fact] void should_require_new_plaintext_consent_on_relogin() => DirectLoginFlow.UseInsecureFileStore(new DirectSettings(), _config, DirectTarget.Create("https://direct.example", "active")).ShouldBeFalse();
}
