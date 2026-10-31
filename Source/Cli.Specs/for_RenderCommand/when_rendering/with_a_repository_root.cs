// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_a_repository_root : given.a_repository_root_render
{
    async Task Because() => _output = await Capture();

    [Fact] void should_succeed() => _output.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_emit_the_bundled_workflow() => File.ReadAllText(_workflow).ShouldContain("dotnet build MyApp.slnx -c Release");
    [Fact] void should_emit_dependency_updates() => File.Exists(Path.Combine(_destination, ".github/dependabot.yml")).ShouldBeTrue();
}
