// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Security;
using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Queries;
using Grpc.Core;

namespace Cratis.Cli.for_WorkbenchDataService.when_fetching_as_a_non_administrator;

public class and_another_failure_occurs : given.a_workbench_data_service
{
    void Establish()
    {
        var unavailable = new RpcException(new Status(StatusCode.Unavailable, "down"));
        _users.AllUsers(Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(Observable.Throw<QueryResult<IEnumerable<UserResponse>>>(unavailable));
    }

    async Task Because() => await Fetch();

    [Fact] void should_not_flag_that_an_administrator_is_required() => _data.AdministratorRequired.ShouldBeFalse();
    [Fact] void should_have_no_users() => _data.Users.ShouldBeEmpty();
}
