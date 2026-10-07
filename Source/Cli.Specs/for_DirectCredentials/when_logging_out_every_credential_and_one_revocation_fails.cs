// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectCredentials;

public class when_logging_out_every_credential_and_one_revocation_fails : given.stored_credentials
{
    readonly a_fake_authorization_server _server = new();
    IReadOnlyList<DirectLogoutOutcome> _outcomes = null!;

    void Establish()
    {
        foreach (var entry in _config.Credentials)
        {
            _server.Store(DirectTarget.Create(entry.Origin, entry.Tenant), $"refresh-{new Uri(entry.Origin).Host}-{entry.Tenant}");
        }

        _server.Stored.Remove(DirectTarget.Create("https://direct.example", null).Key);
        _server.RefusedTokens.Add("refresh-direct.example-previous");
    }

    async Task Because() => _outcomes = await DirectCredentials.Logout(_config, DirectCredentials.Select(_config, null, null, true), _ => _server.Provider(), CancellationToken.None);

    [Fact] void should_revoke_each_revocable_credential() => _server.Revoked.ShouldContainOnly(["refresh-direct.example-active", "refresh-other.example-active"]);
    [Fact] void should_continue_past_the_failure() => _outcomes.Count.ShouldEqual(4);
    [Fact] void should_report_the_failure_without_the_token() => _outcomes.Single(outcome => outcome.Failure is not null).Failure!.ShouldNotContain("refresh-direct.example-previous");
    [Fact] void should_report_the_credential_that_was_not_stored() => _outcomes.Single(outcome => outcome.Credential.Tenant is null).Revoked.ShouldBeFalse();
    [Fact] void should_retain_the_credential_that_was_not_revoked() => _server.Stored.ContainsKey(DirectTarget.Create("https://direct.example", "previous").Key).ShouldBeTrue();
    [Fact] void should_keep_only_the_retained_credential_in_the_index() => _config.Credentials.ShouldContainOnly([Entry("https://direct.example", "previous")]);

    void Destroy() => _server.Dispose();
}
