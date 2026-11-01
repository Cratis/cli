// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_reading_status;

public class after_an_interrupted_update : given.an_interrupted_update
{
    IReadOnlyDictionary<string, DirectMcpClientStatus> _status;

    void Because() => _status = DirectMcpRegistration.Status(DirectMcpScope.User, Locations, ["claude", "cursor"]).ToDictionary(client => client.Client);

    [Fact] void should_report_the_updated_registration() => _status["claude"].Detail.ShouldContain("--tenant team");
    [Fact] void should_report_the_registration_the_update_did_not_reach() => _status["cursor"].Detail.ShouldContain("--no-tenant");
    [Fact] void should_report_both_as_registered() => _status.Values.Select(client => client.State).ShouldContainOnly("registered", "registered");
}
