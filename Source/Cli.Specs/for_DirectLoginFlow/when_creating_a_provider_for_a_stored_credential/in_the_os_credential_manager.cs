// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectLoginFlow.when_creating_a_provider_for_a_stored_credential;

public class in_the_os_credential_manager : Specification
{
    bool? _fileStoreSelected;

    void Because()
    {
        using var http = new HttpClient();
        var entry = new DirectCredentialEntry { Origin = "https://direct.example", Tenant = "team", Issuer = "https://identity.example/", InsecureFileStore = false };
        DirectLoginFlow.ProviderFor(entry, http, insecure =>
        {
            _fileStoreSelected = insecure;
            return Substitute.For<IDirectSecretStore>();
        });
    }

    [Fact] void should_use_the_store_recorded_for_the_credential() => _fileStoreSelected!.Value.ShouldBeFalse();
}
