// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectCredentials;

public class when_logging_out_twice_without_an_explicit_target : given.stored_credentials
{
    readonly a_fake_authorization_server _server = new();
    IReadOnlyList<DirectLogoutOutcome> _second = null!;
    string _noTenantSecret = null!;
    readonly DirectTarget _noTenant = DirectTarget.Create("https://direct.example", null);

    void Establish()
    {
        foreach (var entry in _config.Credentials)
        {
            _server.Store(DirectTarget.Create(entry.Origin, entry.Tenant), $"refresh-{new Uri(entry.Origin).Host}-{entry.Tenant}");
        }

        _noTenantSecret = _server.Stored[_noTenant.Key];
    }

    async Task Because()
    {
        await DirectCredentials.Logout(_config, DirectCredentials.Select(_config, null, null, false), _ => _server.Provider(), CancellationToken.None);
        _second = await DirectCredentials.Logout(_config, DirectCredentials.Select(_config, null, null, false), _ => _server.Provider(), CancellationToken.None);
    }

    [Fact] void should_revoke_only_the_previously_active_credential() => _server.Revoked.ShouldContainOnly(["refresh-direct.example-active"]);
    [Fact] void should_select_nothing_for_the_second_default_logout() => _second.ShouldBeEmpty();
    [Fact] void should_leave_the_no_tenant_secret_unchanged() => _server.Stored[_noTenant.Key].ShouldEqual(_noTenantSecret);
    [Fact] void should_preserve_the_no_tenant_credential_in_the_index() => DirectCredentials.Find(_config, _noTenant).ShouldNotBeNull();
    [Fact] void should_preserve_every_other_stored_credential() => _config.Credentials.Count.ShouldEqual(3);
    [Fact] void should_have_no_active_selection() => _config.HasActiveSelection.ShouldBeFalse();
    [Fact] void should_not_mark_the_no_tenant_credential_active_in_status() => DirectStatusCommand.StoredCredentials(_config, _noTenant).Any(credential => credential.Active).ShouldBeFalse();

    void Destroy() => _server.Dispose();
}
