// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_an_update_adds_copilot_instruction_adapters : given.a_copilot_corpus
{
    string _previousTarget = null!;

    void Establish()
    {
        AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);
        _previousTarget = InstructionsTarget;
        AddAdapter();
    }

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);

    [Fact] void should_start_with_the_legacy_rules_link() => _previousTarget.ShouldEqual("../.cratis/ai/rules");
    [Fact] void should_relink_the_owned_instructions() => InstructionsTarget.ShouldEqual("../.cratis/ai/harnesses/copilot/instructions");
    [Fact] void should_record_the_relink() => _result.Actions.ShouldContain("Configured .github/instructions");
    [Fact] void should_not_report_owned_link_drift() => AiCorpusSynchronizer.Status(_project).ModifiedFiles.ShouldBeEmpty();
    [Fact] void should_have_no_conflicts() => _result.Conflicts.ShouldBeEmpty();
}
