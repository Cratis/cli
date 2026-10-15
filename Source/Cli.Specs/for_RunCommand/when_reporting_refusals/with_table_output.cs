// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_reporting_refusals;

[Collection(CliSpecsCollection.Name)]
public class with_table_output : given.a_refusal_output
{
    void Because() => RunOutput.WriteRefusal(OutputFormats.Table, _status);

    [Fact] void should_show_the_grouped_counts() => _output.ToString().ShouldContain("5 × Persona '…' is authoring metadata.");
    [Fact] void should_show_the_model_fix_total() => _output.ToString().ShouldContain("Fix your model (1)");
    [Fact] void should_render_only_one_error_box() => (_output.ToString().Split(" Error ", StringSplitOptions.None).Length - 1).ShouldEqual(1);
    [Fact] void should_keep_the_final_engine_refusal() => _output.ToString().ShouldContain("The model cannot run on the semantic engine");
    [Fact] void should_not_render_individual_issue_boxes() => _output.ToString().ShouldNotContain("Unsupported(Plan)");
}
