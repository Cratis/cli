// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Cli.for_ChronicleCommand.when_administrator_is_required;

public class and_a_command_not_needing_an_administrator_is_denied : given.a_server_refusing_an_administrative_command
{
    void Establish() => StoreALogin();

    Task Because() => Execute(new RpcException(new Status(StatusCode.PermissionDenied, "denied")), false);

    [Fact] void should_return_a_server_error() => _result.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_not_mention_the_login() => _error.ToString().ShouldNotContain("login");
}
