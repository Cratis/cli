// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectLoginFlow.when_reusing_plaintext_consent;

public class for_a_stored_inactive_target : Specification
{
    bool _allowed;

    void Because() => _allowed = DirectLoginFlow.UseInsecureFileStore(
        new DirectSettings(),
        new DirectConfiguration
        {
            Origin = "https://direct.example",
            Tenant = "active",
            Credentials = [new DirectCredentialEntry { Origin = "https://direct.example", Tenant = "previous", Issuer = "https://identity.example/", InsecureFileStore = true }]
        },
        DirectTarget.Create("https://direct.example", "previous"));

    [Fact] void should_reuse_the_consent_recorded_for_that_target() => _allowed.ShouldBeTrue();
}
