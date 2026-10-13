// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectLoginFlow;

public class when_selecting_the_active_store_with_a_plaintext_flag : Specification
{
    bool _selected;

    void Because()
    {
        using var http = new HttpClient();
        var config = new CliConfiguration
        {
            Direct = new DirectConfiguration
            {
                Origin = "https://direct.example", Tenant = "team", Issuer = "https://identity.example/", InsecureFileStore = true,
                Credentials = [new DirectCredentialEntry { Origin = "https://direct.example", Tenant = "team", Issuer = "https://identity.example/", InsecureFileStore = false }]
            }
        };
        DirectLoginFlow.Active(config, new DirectSettings { InsecureFileStore = true }, http, insecure =>
        {
            _selected = insecure;
            return Substitute.For<IDirectSecretStore>();
        });
    }

    [Fact] void should_use_the_recorded_os_store_instead_of_the_flag_or_selection() => _selected.ShouldBeFalse();
}
