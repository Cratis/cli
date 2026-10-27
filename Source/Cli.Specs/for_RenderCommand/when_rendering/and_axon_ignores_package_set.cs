// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_axon_ignores_package_set : given.a_check_command
{
    (int ExitCode, string Stdout, string Stderr) _output;

    void Establish()
    {
        _settings.Target = "axon";
        _settings.PackageSet = Path.Combine(_folder, "missing-packages.json");
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_not_read_the_ignored_package_set() => _output.ExitCode.ShouldEqual(ExitCodes.ChangesPending);
    [Fact] void should_explain_the_ignored_option() => _output.Stderr.ShouldContain("ignores --package-set");
    [Fact] void should_report_one_experimental_warning() => _output.Stderr.Split("CLI-RENDER-029", StringSplitOptions.None).Length.ShouldEqual(2);
}
