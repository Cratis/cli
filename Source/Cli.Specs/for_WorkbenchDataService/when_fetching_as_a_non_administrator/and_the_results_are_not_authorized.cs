// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Security;
using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Cli.for_WorkbenchDataService.when_fetching_as_a_non_administrator;

public class and_the_results_are_not_authorized : given.a_workbench_data_service
{
    void Establish()
    {
        _users.AllUsers(Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(Observable.Return(new QueryResult<IEnumerable<UserResponse>> { IsAuthorized = false }));
        _applications.AllApplications(Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(Observable.Return(new QueryResult<IEnumerable<ApplicationResponse>> { IsAuthorized = false }));
    }

    async Task Because() => await Fetch();

    [Fact] void should_flag_that_users_require_an_administrator() => _data.UsersRequireAdministrator.ShouldBeTrue();
    [Fact] void should_flag_that_applications_require_an_administrator() => _data.ApplicationsRequireAdministrator.ShouldBeTrue();
    [Fact] void should_have_no_users() => _data.Users.ShouldBeEmpty();
}
