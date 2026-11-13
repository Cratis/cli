// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Security;
using Grpc.Core;

namespace Cratis.Cli.for_WorkbenchDataService.when_fetching_as_a_non_administrator;

public class and_the_server_answers_unauthenticated : given.a_workbench_data_service
{
    void Establish()
    {
        var unauthenticated = new RpcException(new Status(StatusCode.Unauthenticated, "token rejected"));
        _users.AllUsers(Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(Observable.Throw<QueryResult<IEnumerable<UserResponse>>>(unauthenticated));
        _applications.AllApplications(Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(Observable.Throw<QueryResult<IEnumerable<ApplicationResponse>>>(unauthenticated));
    }

    async Task Because() => await Fetch();

    [Fact] void should_show_the_login_guidance_for_users() => _data.UsersRequireAdministrator.ShouldBeTrue();
    [Fact] void should_show_the_login_guidance_for_applications() => _data.ApplicationsRequireAdministrator.ShouldBeTrue();
    [Fact] void should_have_no_users() => _data.Users.ShouldBeEmpty();
    [Fact] void should_have_no_applications() => _data.Applications.ShouldBeEmpty();
}
