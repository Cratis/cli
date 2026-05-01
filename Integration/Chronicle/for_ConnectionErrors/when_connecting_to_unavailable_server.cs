// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Cli.Integration.Chronicle.for_ConnectionErrors.when_connecting_to_unavailable_server.context;

namespace Cratis.Cli.Integration.Chronicle.for_ConnectionErrors;

[Collection(ChronicleCollection.Name)]
public class when_connecting_to_unavailable_server(context context) : CliGiven<context>(context)
{
    public class context : Specification
    {
        public CliCommandResult Result = null!;

        async Task Because() => Result = await CliCommandRunner.RunAsync("chronicle", "event-stores", "list", "--server", "chronicle://dev:devsecret@localhost:19999", "--output", "json");
    }

    [Fact] void should_return_connection_error_exit_code() => Context.Result.ExitCode.ShouldEqual(ExitCodes.ConnectionError);

    [Fact] void should_output_connection_error_code_to_stderr() => Context.Result.StandardError.ShouldContain("connection_error");
}
