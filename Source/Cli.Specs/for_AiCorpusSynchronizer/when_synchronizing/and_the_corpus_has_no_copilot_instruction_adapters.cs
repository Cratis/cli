// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_the_corpus_has_no_copilot_instruction_adapters : given.a_copilot_corpus
{
    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);

    [Fact] void should_fall_back_to_canonical_rules() => InstructionsTarget.ShouldEqual("../.cratis/ai/rules");
    [Fact] void should_have_no_conflicts() => _result.Conflicts.ShouldBeEmpty();
}
