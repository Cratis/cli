// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_invalid_reducer_configuration : given.a_reducer_render_command
{
    [Theory]
    [InlineData("not json", false)]
    [InlineData("not json", true)]
    [InlineData("{\"version\":1,\"application\":\"OtherApp\",\"reducerConvention\":\"self\"}", false)]
    [InlineData("{\"version\":1,\"application\":\"OtherApp\",\"reducerConvention\":\"self\"}", true)]
    [InlineData("{\"version\":2,\"application\":\"MyApp\",\"reducerConvention\":\"self\"}", false)]
    [InlineData("{\"version\":2,\"application\":\"MyApp\",\"reducerConvention\":\"self\"}", true)]
    public async Task should_refuse_before_planning_or_writing(string json, bool check)
    {
        WriteConfiguration(json);
        _settings.Check = check;
        var before = Snapshot(_destination);

        var output = await Capture();

        output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
        output.Stderr.ShouldContain("CLI-RENDER-021");
        _requests.ShouldBeEmpty();
        Snapshot(_destination).ShouldEqual(before);
    }
}
