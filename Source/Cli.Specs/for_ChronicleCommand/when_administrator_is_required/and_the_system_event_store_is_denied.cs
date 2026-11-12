// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Grpc.Core;

namespace Cratis.Cli.for_ChronicleCommand.when_administrator_is_required;

public class and_the_system_event_store_is_denied : given.a_server_refusing_an_administrative_command
{
    void Establish() => StoreALogin();

    async Task Because() => await Execute(
        new RpcException(new Status(StatusCode.PermissionDenied, "Administrator access required")),
        requiresAdministrator: false,
        eventStore: "system");

    [Fact] void should_report_a_server_error() => _result.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_identify_the_permission_denial() => JsonElement.Parse(_error.ToString()).GetProperty("message").GetString().ShouldContain("Administrator access required");
    [Fact] void should_point_at_the_login_command() => _error.ToString().ShouldContain("cratis chronicle login");
    [Fact] void should_not_report_a_json_parse_error() => _error.ToString().ShouldNotContain("JSON");
}
