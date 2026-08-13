// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_an_output_directory_is_given : given.a_render_command
{
    int _result;

    void Establish()
    {
        _settings.Path = "MyApp.play";
        _settings.Target = "src/MyApp";
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_render_into_it() => _rendering.Received(1).Render(_document, Path.Combine(_folder, "src", "MyApp"));
}
