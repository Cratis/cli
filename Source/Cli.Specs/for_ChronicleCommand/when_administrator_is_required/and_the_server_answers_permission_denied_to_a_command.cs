// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Cli.for_ChronicleCommand.when_administrator_is_required;

public class and_the_server_answers_permission_denied_to_a_command : given.a_server_refusing_an_administrative_command
{
    void Establish() => StoreALogin();

    Task Because() => Execute(new RpcException(new Status(StatusCode.PermissionDenied, "denied")), true);

    [Fact] void should_return_a_server_error() => _result.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_say_an_administrator_is_needed() => _error.ToString().ShouldContain("administrator");
    [Fact] void should_name_the_login_command() => _error.ToString().ShouldContain("cratis chronicle login");
}
