// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_a_scoped_repository_root : given.a_reducer_render_command
{
    void Establish()
    {
        _settings.RepositoryRoot = true;
        _settings.Scope = ["Orders.Ordering.PlaceOrder"];
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_succeed() => _output.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_carry_the_option_to_the_fresh_scaffold() => _plans.Single().ScaffoldOptions!.RepositoryRoot.ShouldBeTrue();
    [Fact] void should_emit_repository_ci_with_the_destination_profile() => File.Exists(Path.Combine(_destination, ".github/workflows/build.yml")).ShouldBeTrue();
    [Fact] void should_keep_the_reducer_configuration() => File.Exists(_configuration).ShouldBeTrue();
}
