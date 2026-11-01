// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_repository_root_is_not_supported : given.a_repository_root_render
{
    void Establish() => _settings.Target = "axon";

    async Task Because() => _output = await Capture();

    [Fact] void should_refuse_the_option() => _output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_a_typed_diagnostic() => _output.Stderr.ShouldContain("CLI-RENDER-032");
    [Fact] void should_not_publish() => Directory.Exists(_destination).ShouldBeFalse();
}
