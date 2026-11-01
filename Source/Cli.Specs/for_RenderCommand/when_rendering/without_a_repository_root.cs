// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class without_a_repository_root : given.a_repository_root_render
{
    void Establish() => _settings.RepositoryRoot = new RenderSettings().RepositoryRoot;

    async Task Because() => _output = await Capture();

    [Fact] void should_succeed() => _output.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_not_emit_repository_ci() => Directory.Exists(Path.Combine(_destination, ".github")).ShouldBeFalse();
    [Fact] void should_keep_the_application_scaffold() => File.Exists(Path.Combine(_destination, "MyApp.csproj")).ShouldBeTrue();
}
