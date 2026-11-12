// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Cli.for_ChronicleCommand.when_administrator_is_required;

public class and_the_stored_token_is_rejected_as_unauthenticated : given.a_server_refusing_an_administrative_command
{
    void Establish() => StoreALogin();

    Task Because() => Execute(new RpcException(new Status(StatusCode.Unauthenticated, "token rejected")), true);

    [Fact] void should_return_an_authentication_error() => _result.ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_not_claim_an_administrator_is_needed() => _error.ToString().ShouldNotContain("administrator");
    [Fact] void should_ask_for_a_new_login() => _error.ToString().ShouldContain("cratis chronicle login");
}
