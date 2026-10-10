// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AuthStatusCommand.when_reporting_status;

[Collection(CliSpecsCollection.Name)]
public class with_equivalent_ipv6_addresses : given.an_auth_status
{
    void Establish()
    {
        _configuration.Contexts["default"].TokenServer = "[2001:db8::1]:35000";
        _settings.Server = "chronicle://[2001:DB8:0:0:0:0:0:1]:35000";
    }

    async Task Because() => await Execute();

    [Fact] void should_report_the_match() => _status.RootElement.GetProperty("loginMatchesServer").GetBoolean().ShouldBeTrue();
}
