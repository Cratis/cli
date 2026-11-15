// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Cli.Integration.Chronicle.for_Users.when_listing_users_with_application_credentials.context;

namespace Cratis.Cli.Integration.Chronicle.for_Users;

[Collection(ChronicleCollection.Name)]
public class when_listing_users_with_application_credentials(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult Result = null!;

        async Task Because() => Result = await RunCliAsync("chronicle", "users", "list");
    }

    [Fact] void should_report_a_server_error() => Context.Result.ExitCode.ShouldEqual(ExitCodes.ServerError);

    [Fact] void should_point_at_the_login_command() => Context.Result.StandardError.ShouldContain("cratis chronicle login");
}
