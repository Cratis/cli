// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Cli.for_ChronicleCommand.when_administrator_is_required;

public class and_the_system_event_store_failure_is_not_a_denial : given.a_server_refusing_an_administrative_command
{
    void Establish() => StoreALogin();

    async Task Because() => await Execute(
        new RpcException(new Status(StatusCode.Internal, "Operation failed")),
        requiresAdministrator: false,
        eventStore: "system");

    [Fact] void should_report_a_server_error() => _result.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_surface_the_server_failure() => _error.ToString().ShouldContain("Operation failed");
    [Fact] void should_not_suggest_an_administrator_login() => _error.ToString().ShouldNotContain("cratis chronicle login");
}
