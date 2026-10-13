// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectCredentials;

public class when_listing_stored_credentials : given.stored_credentials
{
    IReadOnlyList<DirectStoredCredential> _listed = null!;

    void Establish() => _config.Credentials[1].InsecureFileStore = true;

    void Because() => _listed = DirectStatusCommand.StoredCredentials(_config, DirectTarget.Create("https://direct.example", "active"));

    [Fact] void should_list_every_stored_target() => _listed.Count.ShouldEqual(4);
    [Fact] void should_mark_only_the_active_target() => _listed.Single(credential => credential.Active).ShouldEqual(new DirectStoredCredential("https://direct.example", "active", "os", true));
    [Fact] void should_name_the_plaintext_store() => _listed.Single(credential => credential.Tenant == "previous").Store.ShouldEqual("file");
}
