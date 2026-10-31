// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_checking;

[Collection(CliSpecsCollection.Name)]
public class and_repository_root_is_parsed : given.a_repository_root_render
{
    [Fact]
    public async Task should_accept_the_opt_in_without_writing()
    {
        var result = await CliApp.Create().RunAsync(
            ["render", _folder, "--name", "MyApp", "--destination", _destination, "--repository-root", "--check", "-o", "json-compact"],
            TestContext.Current.CancellationToken);

        result.ShouldEqual(ExitCodes.ChangesPending);
        Directory.Exists(_destination).ShouldBeFalse();
    }
}
