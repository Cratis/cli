// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli;
using context = Cratis.Integration.Chronicle.Cli.for_EventStores.when_listing_event_stores.context;

namespace Cratis.Integration.Chronicle.Cli.for_EventStores;

[Collection(ChronicleCollection.Name)]
public class when_listing_event_stores(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult Result = null!;

        async Task Because() => Result = await RunCliAsync("chronicle", "event-stores", "list");
    }

    [Fact] void should_return_success_exit_code() => Context.Result.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_contain_system_event_store() => Context.Result.StandardOutput.Contains("System", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();

    [Fact] void should_have_no_errors() => Context.Result.StandardError.ShouldEqual(string.Empty);
}
