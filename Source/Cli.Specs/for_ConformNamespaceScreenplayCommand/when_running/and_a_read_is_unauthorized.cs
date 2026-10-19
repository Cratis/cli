// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformNamespaceScreenplayCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class and_a_read_is_unauthorized : given.a_conform_namespace_command
{
    void Establish() => _observations.RegisteredEventTypes("Library", Arg.Any<CancellationToken>()).Returns<Task<IReadOnlyList<Cratis.Screenplay.Observations.ObservedEventType>>>(_ => throw new ChronicleReadFailed("event types", true, "not authorized"));

    async Task Because() => await Run();

    [Fact] void should_report_an_authentication_error() => _exitCode.ShouldEqual(ExitCodes.AuthenticationError);
}
