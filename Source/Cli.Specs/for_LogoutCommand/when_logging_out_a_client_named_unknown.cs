// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LogoutCommand;

[Collection(CliSpecsCollection.Name)]
public class when_logging_out_a_client_named_unknown : given.a_logout_command
{
    string _first;
    string _second;
    string _legacy;

    void Establish()
    {
        var config = CliConfiguration.Load();
        config.GetCurrentContext().ClientId = "unknown";
        config.Save();
        _first = CachePath(Connection(client: "unknown"));
        _second = CachePath(Connection(disableTls: true, client: "unknown"));
        _legacy = CliConfiguration.GetTokenCachePath($"{Context}_unknown");
        Seed(_first);
        Seed(_second);
        Seed(_legacy);
    }

    Task Because() => Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_clear_the_first_destination() => File.Exists(_first).ShouldBeFalse();
    [Fact] void should_clear_the_second_destination() => File.Exists(_second).ShouldBeFalse();
    [Fact] void should_clear_the_legacy_entry() => File.Exists(_legacy).ShouldBeFalse();
}
