// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_unowned_reducer_configuration_in_an_existing_application : given.a_reducer_render_command
{
    async Task Establish()
    {
        await PublishClassic();
        WriteConfiguration("{\"version\":1,\"application\":\"MyApp\",\"reducerConvention\":\"self\"}");
        _settings.Force = true;
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(false, "Orders.Ordering.Totals")]
    public async Task should_refuse_to_switch_the_convention(bool check, string? scope)
    {
        _settings.Check = check;
        _settings.Scope = scope is null ? [] : [scope];
        var before = Snapshot(_destination);

        var output = await Capture();

        output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
        output.Stderr.ShouldContain("CLI-RENDER-023");
        _requests.ShouldBeEmpty();
        Snapshot(_destination).ShouldEqual(before);
    }
}
