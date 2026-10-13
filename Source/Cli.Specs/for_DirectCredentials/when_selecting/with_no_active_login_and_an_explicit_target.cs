// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectCredentials.when_selecting;

public class with_no_active_login_and_an_explicit_target : given.stored_credentials
{
    IReadOnlyList<DirectCredentialEntry> _noTenant = null!;
    IReadOnlyList<DirectCredentialEntry> _tenant = null!;
    IReadOnlyList<DirectCredentialEntry> _all = null!;
    IReadOnlyList<DirectCredentialEntry> _allOnOrigin = null!;

    void Establish() => DirectCredentials.Forget(_config, _config.Credentials.Single(entry => entry.Origin == _config.Origin && entry.Tenant == _config.Tenant));

    void Because()
    {
        _noTenant = DirectCredentials.Select(_config, "https://direct.example", null, false);
        _tenant = DirectCredentials.Select(_config, null, "previous", false);
        _all = DirectCredentials.Select(_config, null, null, true);
        _allOnOrigin = DirectCredentials.Select(_config, "https://direct.example", null, true);
    }

    [Fact] void should_allow_explicit_origin_selection_of_the_no_tenant_credential() => _noTenant.ShouldContainOnly([Entry("https://direct.example", null)]);
    [Fact] void should_allow_explicit_tenant_selection_on_the_previous_origin() => _tenant.ShouldContainOnly([Entry("https://direct.example", "previous")]);
    [Fact] void should_allow_all_credentials_to_be_selected() => _all.Count.ShouldEqual(3);
    [Fact] void should_allow_all_credentials_on_an_explicit_origin_to_be_selected() => _allOnOrigin.ShouldContainOnly([Entry("https://direct.example", "previous"), Entry("https://direct.example", null)]);
}
