// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformScreenplayCommand.when_conforming;

[Collection(CliSpecsCollection.Name)]
public class and_the_project_cannot_be_resolved : given.a_conform_command
{
    void Establish() => _settings.Project = Path.Combine(_folder, "missing.csproj");
    async Task Because() => await Execute();
    [Fact] void should_report_that_the_check_could_not_run() => _exitCode.ShouldEqual(2);
    [Fact] void should_not_generate() => _generation.DidNotReceive().Generate(Arg.Any<string>(), Arg.Any<ScreenplayGenerationOptions>(), Arg.Any<Action<string>>(), Arg.Any<CancellationToken>());
}
