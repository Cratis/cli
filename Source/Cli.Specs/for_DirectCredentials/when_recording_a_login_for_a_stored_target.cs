// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectCredentials;

public class when_recording_a_login_for_a_stored_target : given.stored_credentials
{
    void Because() => DirectCredentials.Record(_config, new DirectCredentialEntry { Origin = "https://direct.example", Tenant = "previous", Issuer = "https://identity.example/", InsecureFileStore = true });

    [Fact] void should_keep_one_entry_per_target() => _config.Credentials.Count(entry => entry.Origin == "https://direct.example" && entry.Tenant == "previous").ShouldEqual(1);
    [Fact] void should_record_the_new_store_choice() => _config.Credentials.Single(entry => entry.Tenant == "previous").InsecureFileStore.ShouldBeTrue();
}
