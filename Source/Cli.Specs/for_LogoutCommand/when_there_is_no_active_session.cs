// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LogoutCommand;

[Collection(CliSpecsCollection.Name)]
public class when_there_is_no_active_session : given.a_logout_command
{
    string _cache;

    void Establish()
    {
        new CliConfiguration().Save();
        _cache = CachePath(Connection());
        Seed(_cache);
    }

    Task Because() => Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_not_remove_an_unselected_identity() => File.Exists(_cache).ShouldBeTrue();
}
