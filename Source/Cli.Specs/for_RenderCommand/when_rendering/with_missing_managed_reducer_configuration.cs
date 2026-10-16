// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_missing_managed_reducer_configuration : given.a_reducer_render_command
{
    async Task Establish()
    {
        var first = await Capture();
        first.ExitCode.ShouldEqual(ExitCodes.Success);
        File.Delete(_configuration);
        _requests.Clear();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_refuse_to_guess_the_convention(bool check)
    {
        _settings.Check = check;
        var before = Snapshot(_destination);

        var output = await Capture();

        output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
        output.Stderr.ShouldContain("CLI-RENDER-021");
        _requests.ShouldBeEmpty();
        Snapshot(_destination).ShouldEqual(before);
    }
}
