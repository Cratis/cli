// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_an_old_cli_recorded_copilot_instructions_from_rules : given.an_old_copilot_installation
{
    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);

    [Fact] void should_reconfigure_the_owned_link() => InstructionsTarget.ShouldEqual("../.cratis/ai/harnesses/copilot/instructions");
    [Fact] void should_record_the_reconfiguration() => _result.Actions.ShouldContain("Configured .github/instructions");
    [Fact] void should_have_no_conflicts() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_accept_the_updated_manifest() => AiCorpusSynchronizer.Status(_project).ModifiedFiles.ShouldBeEmpty();
}
