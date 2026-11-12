// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Cli.for_ChronicleCommand.when_administrator_is_required;

public class and_the_server_answers_permission_denied : Specification
{
    bool _permissionDenied;
    bool _unauthenticated;
    bool _httpUnauthorized;
    bool _unavailable;

    void Because()
    {
        _permissionDenied = AdministratorRequired.IsDenial(new RpcException(new Status(StatusCode.PermissionDenied, "denied")));
        _unauthenticated = AdministratorRequired.IsDenial(new RpcException(new Status(StatusCode.Unauthenticated, "no principal")));
        _httpUnauthorized = AdministratorRequired.IsDenial(new RpcException(new Status(StatusCode.Unauthenticated, "Bad gRPC response. HTTP status code: 401")));
        _unavailable = AdministratorRequired.IsDenial(new RpcException(new Status(StatusCode.Unavailable, "down")));
    }

    [Fact] void should_treat_permission_denied_as_a_denial() => _permissionDenied.ShouldBeTrue();
    [Fact] void should_treat_unauthenticated_as_a_denial() => _unauthenticated.ShouldBeTrue();
    [Fact] void should_leave_http_401_to_the_token_handling() => _httpUnauthorized.ShouldBeFalse();
    [Fact] void should_not_treat_other_failures_as_a_denial() => _unavailable.ShouldBeFalse();
}
