// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_the_path_cannot_be_resolved : given.a_render_command
{
    int _result;

    void Establish() => _settings.Path = "Missing/MyApp.play";

    async Task Because() => _result = await Execute();

    [Fact] void should_report_that_it_was_not_found() => _result.ShouldEqual(ExitCodes.NotFound);
    [Fact] void should_not_render_anything() => _rendering.DidNotReceive().Render(Arg.Any<string>(), Arg.Any<string>());
}
