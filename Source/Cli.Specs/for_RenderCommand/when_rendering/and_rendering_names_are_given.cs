// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_rendering_names_are_given : given.a_render_command
{
    [Theory]
    [InlineData("Delivery.Backend", "Company.Projects")]
    [InlineData("Delivery.Backend", null)]
    [InlineData(null, "Company.Projects")]
    public async Task should_forward_rendering_choices_without_changing_application_identity(string? projectName, string? rootNamespace)
    {
        _settings.Path = "MyApp.play";
        _settings.ProjectName = projectName;
        _settings.RootNamespace = rootNamespace;

        var result = await Execute();

        result.ShouldEqual(ExitCodes.Success);
        await _planning.Received(1).Plan(
            Arg.Is<ScreenplayRenderRequest>(_ => _.SourcePath == _document && _.ApplicationName == "MyApp" &&
                _.Target == RenderCommand.DefaultRendererTarget && _.ProjectName == projectName && _.RootNamespace == rootNamespace),
            Arg.Any<CancellationToken>());
    }
}
