// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Cli.Integration.Chronicle.for_Events.when_tailing_events.context;

namespace Cratis.Cli.Integration.Chronicle.for_Events;

[Collection(ChronicleCollection.Name)]
public class when_tailing_events(context context) : CliGiven<context>(context)
{
    public class context : given.a_connected_cli
    {
        public CliCommandResult Result = null!;

        async Task Because() => Result = await RunCliAsync("chronicle", "events", "tail", "--event-store", "system");
    }

    [Fact] void should_return_success_exit_code() => Context.Result.ExitCode.ShouldEqual(ExitCodes.Success);

    [Fact] void should_return_json_with_tail_sequence_number() => Context.Result.StandardOutput.ShouldContain("tailSequenceNumber");

    [Fact] void should_have_no_errors() => Context.Result.StandardError.ShouldEqual(string.Empty);
}
