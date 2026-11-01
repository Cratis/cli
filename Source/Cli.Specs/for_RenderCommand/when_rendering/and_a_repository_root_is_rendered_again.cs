// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_a_repository_root_is_rendered_again : given.a_repository_root_render
{
    string[] _before = [];

    async Task Establish()
    {
        (await Capture()).ExitCode.ShouldEqual(ExitCodes.Success);
        _before = Snapshot(_destination);
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_succeed() => _output.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_preserve_every_byte() => Snapshot(_destination).ShouldEqual(_before);
}
