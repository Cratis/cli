// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Cli.for_ChronicleCommand.when_administrator_is_required;

public class and_client_credentials_are_rejected_as_unauthenticated : given.a_server_refusing_an_administrative_command
{
    Task Because() => Execute(new RpcException(new Status(StatusCode.Unauthenticated, "bad client")), true, "chronicle://override:35001?apiKey=embedded-key");

    [Fact] void should_return_an_authentication_error() => _result.ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_not_claim_an_administrator_is_needed() => _error.ToString().ShouldNotContain("administrator");
}
