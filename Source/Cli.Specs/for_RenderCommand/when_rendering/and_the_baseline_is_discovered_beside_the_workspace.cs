// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_the_baseline_is_discovered_beside_the_workspace : given.a_baseline_workspace
{
    void Establish()
    {
        File.Move(_baselinePath, _implicitPath);
        _settings.Baseline = null;
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_return_success() => _output.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_the_discovered_violation() => _output.Stderr.ShouldContain("STAGE-BASELINE-001");
}
