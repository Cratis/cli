// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageContainer.when_building_run_arguments;

public class with_remote_access : Specification
{
    [Theory]
    [InlineData(false, false, "127.0.0.1")]
    [InlineData(true, false, "127.0.0.1")]
    [InlineData(false, true, "0.0.0.0")]
    [InlineData(true, true, "0.0.0.0")]
    void should_publish_both_ports_on_the_selected_address(bool file, bool allowRemote, string address)
    {
        var arguments = file
            ? StageContainer.BuildRunArgumentsForFile("/work/model.play", "latest", 9191, 35001, "sandbox")
            : StageContainer.BuildRunArguments("/work/models", "latest", 9191, 35001, "sandbox");
        arguments = StageContainer.BindStandalonePorts(arguments, allowRemote, runtime: null);
        arguments.ShouldContain($"{address}:9191:9090");
        arguments.ShouldContain($"{address}:35001:35000");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_keep_explicit_runtime_bindings_authoritative(bool file)
    {
        var runtime = new StageRuntimeOptions("network", "stage", "127.0.0.1", [], []);
        var arguments = file
            ? StageContainer.BuildRunArgumentsForFile("/work/model.play", "latest", 9191, 35001, "sandbox", runtime: runtime)
            : StageContainer.BuildRunArguments("/work/models", "latest", 9191, 35001, "sandbox", runtime: runtime);
        arguments = StageContainer.BindStandalonePorts(arguments, allowRemote: true, runtime);
        arguments.ShouldContain("127.0.0.1:9191:9090");
        arguments.ShouldContain("127.0.0.1:35001:35000");
        arguments.ShouldNotContain("0.0.0.0:9191:9090");
    }
}
