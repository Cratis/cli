// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectCredentials;

public class when_logging_out_an_active_no_tenant_login : given.stored_credentials
{
    readonly a_fake_authorization_server _server = new();
    IReadOnlyList<DirectLogoutOutcome> _outcomes = null!;
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", null);

    void Establish()
    {
        _config.Tenant = null;
        _server.Store(_target, "no-tenant-refresh");
    }

    async Task Because() => _outcomes = await DirectCredentials.Logout(_config, DirectCredentials.Select(_config, null, null, false), _ => _server.Provider(), CancellationToken.None);

    [Fact] void should_revoke_the_active_no_tenant_login() => _server.Revoked.ShouldContainOnly(["no-tenant-refresh"]);
    [Fact] void should_report_one_successful_logout() => _outcomes.Single().Revoked.ShouldBeTrue();
    [Fact] void should_remove_the_no_tenant_secret() => _server.Stored.ContainsKey(_target.Key).ShouldBeFalse();
    [Fact] void should_remove_the_no_tenant_index_entry() => DirectCredentials.Find(_config, _target).ShouldBeNull();
    [Fact] void should_clear_the_active_selection() => _config.HasActiveSelection.ShouldBeFalse();
    [Fact] void should_select_nothing_after_logout() => DirectCredentials.Select(_config, null, null, false).ShouldBeEmpty();

    void Destroy() => _server.Dispose();
}
