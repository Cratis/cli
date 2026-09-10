// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Cli.Integration.Chronicle.for_Diagnose.when_diagnosing_server.context;

namespace Cratis.Cli.Integration.Chronicle.for_Diagnose;

[Collection(ChronicleCollection.Name)]
public class when_diagnosing_server(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult Result = null!;

        async Task Because() => Result = await RunCliAsync("chronicle", "diagnose");
    }

    [Fact] void should_include_event_stores() => Context.Result.StandardOutput.ShouldContain("\"eventStores\": [");
    [Fact] void should_include_the_system_event_store() => Context.Result.StandardOutput.ShouldContain("System");
    [Fact] void should_have_no_errors() => Context.Result.StandardError.ShouldEqual(string.Empty);
}
