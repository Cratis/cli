// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LogoutCommand;

[Collection(CliSpecsCollection.Name)]
public class when_logging_out : given.a_logout_command
{
    string _first;
    string _second;
    string _otherClient;
    string _otherContext;
    string _legacy;
    string _notAToken;
    bool _otherCredentialsPreserved;

    void Establish()
    {
        _first = CachePath(Connection());
        _second = CachePath(Connection(disableTls: true));
        _otherClient = CachePath(Connection(client: "client-other"));
        _otherContext = CachePath(Connection(), "other");
        _legacy = CliConfiguration.GetTokenCachePath($"{Context}_client");
        _notAToken = _first + ".unrelated";
        foreach (var path in new[] { _first, _second, _otherClient, _otherContext, _legacy, _notAToken })
        {
            Seed(path);
        }
    }

    async Task Because()
    {
        await Execute();
        _otherCredentialsPreserved = CliConfiguration.Load().Contexts["other"].ClientSecret == "other-fixture";
    }

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_clear_all_active_context_credentials() => _cleared.ShouldBeTrue();
    [Fact] void should_delete_the_first_destination() => File.Exists(_first).ShouldBeFalse();
    [Fact] void should_delete_the_second_destination() => File.Exists(_second).ShouldBeFalse();
    [Fact] void should_delete_the_legacy_entry() => File.Exists(_legacy).ShouldBeFalse();
    [Fact] void should_preserve_other_clients() => File.Exists(_otherClient).ShouldBeTrue();
    [Fact] void should_preserve_other_contexts() => File.Exists(_otherContext).ShouldBeTrue();
    [Fact] void should_preserve_non_token_files() => File.Exists(_notAToken).ShouldBeTrue();
    [Fact] void should_preserve_other_context_credentials() => _otherCredentialsPreserved.ShouldBeTrue();
}
