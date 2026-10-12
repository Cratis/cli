// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_the_corpus_ships_copilot_instruction_adapters : given.a_copilot_corpus
{
    void Establish() => AddAdapter();
    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);

    [Fact] void should_link_the_native_instructions() => InstructionsTarget.ShouldEqual("../.cratis/ai/harnesses/copilot/instructions");
    [Fact] void should_install_the_adapter() => File.Exists(Path.Combine(_project, ".github", "instructions", "general.instructions.md")).ShouldBeTrue();
    [Fact] void should_have_no_conflicts() => _result.Conflicts.ShouldBeEmpty();
}
