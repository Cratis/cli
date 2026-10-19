// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformNamespaceScreenplayCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class and_a_read_is_refused : given.a_conform_namespace_command
{
    void Establish() => _observations.Tail("Library", "Default", "event-log", Arg.Any<CancellationToken>()).Returns<Task<ulong?>>(_ => throw new ChronicleReadFailed("tail sequence number", false, "storage failed"));

    async Task Because() => await Run();

    [Fact] void should_report_a_server_error() => _exitCode.ShouldEqual(ExitCodes.ServerError);
    [Fact] void should_not_write_a_report() => (_output + _error).ShouldNotContain("findings");
}
