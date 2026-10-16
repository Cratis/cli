// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_edited_managed_reducer_configuration : given.a_reducer_render_command
{
    async Task Establish()
    {
        var first = await Capture();
        first.ExitCode.ShouldEqual(ExitCodes.Success);
        WriteConfiguration("{\"version\":1,\"application\":\"MyApp\",\"reducerConvention\":\"classic\"}");
        _requests.Clear();
        _settings.Force = true;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_refuse_migration_even_with_force(bool check)
    {
        _settings.Check = check;
        var before = Snapshot(_destination);

        var output = await Capture();

        output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
        output.Stderr.ShouldContain("CLI-RENDER-023");
        _requests.ShouldBeEmpty();
        Snapshot(_destination).ShouldEqual(before);
    }
}
