// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformScreenplayCommand.when_conforming;

[Collection(CliSpecsCollection.Name)]
public class and_json_output_is_requested : given.a_conform_command
{
    void Establish() => _settings.Output = OutputFormats.Json;
    async Task Because() => await Execute();
    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_report_model_and_project() => _output.GetProperty("modelRoot").GetString().ShouldEqual(_model);
    [Fact] void should_report_the_projects_read() => Findings("projects").Single().GetString().ShouldEqual("Library");
    [Fact] void should_report_compared_counts() => _output.GetProperty("comparedCounts").GetProperty("model").GetInt32().ShouldEqual(_output.GetProperty("comparedCounts").GetProperty("code").GetInt32());
    [Fact] void should_report_limits() => Findings("notCompared").ShouldNotBeEmpty();
    [Fact] void should_count_generation_diagnostics() => _output.GetProperty("generationDiagnostics").GetInt32().ShouldEqual(0);
}
