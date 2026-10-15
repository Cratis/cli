// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_reporting_refusals;

[Collection(CliSpecsCollection.Name)]
public class with_plain_output : given.a_refusal_output
{
    void Because() => RunOutput.WriteRefusal(OutputFormats.Plain, _status);

    [Fact] void should_show_category_totals() => _output.ToString().ShouldContain("Ignored by this engine (5)");
    [Fact] void should_show_a_kind_once_with_its_count() => _output.ToString().ShouldContain("5 × Persona '…' is authoring metadata.");
    [Fact] void should_limit_locations() => _output.ToString().ShouldContain("at one.play(1,1), two.play(2,1), three.play(3,1) (+2 more)");
    [Fact] void should_put_model_fixes_before_engine_gaps_and_metadata() => (_output.ToString().IndexOf("Fix your model", StringComparison.Ordinal) < _output.ToString().IndexOf("Not supported by this engine yet", StringComparison.Ordinal) && _output.ToString().IndexOf("Not supported by this engine yet", StringComparison.Ordinal) < _output.ToString().IndexOf("Ignored by this engine", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_list_each_issue_by_default() => _output.ToString().ShouldNotContain("Unsupported(Plan)");
    [Fact] void should_keep_the_final_error_and_suggestion() => _error.ToString().ShouldContain("The model cannot run on the semantic engine");
}
