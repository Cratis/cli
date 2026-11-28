// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_McpHttpBridge;

[Collection(CliSpecsCollection.Name)]
public class when_launching_a_local_tool : Cratis.Cli.Commands.Build.given.a_builder_process
{
    [Cratis.Cli.for_DirectFileSecrets.given.unix_only.Fact]
    async Task should_filter_the_environment_before_starting_the_server()
    {
        var program = Script("tool", Check + " && touch clean");
        await using var bridge = McpHttpBridge.Start(program, [], Folder);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!File.Exists(Path.Combine(Folder, "clean"))) await Task.Delay(10, deadline.Token);
    }
}
