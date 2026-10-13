// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AuthStatusCommand.when_reporting_status;

[Collection(CliSpecsCollection.Name)]
public class with_multiple_hosts : given.an_auth_status
{
    void Establish() => _settings.Server = "chronicle://production:35000,production:35000";
    async Task Because() => await Execute();

    [Fact] void should_not_match_even_if_each_host_matches() => _status.RootElement.GetProperty("loginMatchesServer").GetBoolean().ShouldBeFalse();
}
