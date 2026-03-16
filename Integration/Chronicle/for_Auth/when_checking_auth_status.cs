// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli;
using context = Cratis.Integration.Chronicle.Cli.for_Auth.when_checking_auth_status.context;

namespace Cratis.Integration.Chronicle.Cli.for_Auth;

[Collection(ChronicleCollection.Name)]
public class when_checking_auth_status(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult Result = null!;

        async Task Because() => Result = await RunCliAsync("chronicle", "auth", "status");
    }

    [Fact] void should_return_success_exit_code() => Context.Result.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_have_output() => (Context.Result.StandardOutput.Length > 0).ShouldBeTrue();

    [Fact] void should_have_no_errors() => Context.Result.StandardError.ShouldEqual(string.Empty);
}
