// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_the_corpus_ships_pi_agent_adapters : given.a_pi_corpus
{
    void Establish() => AddAdapter();

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);

    [Fact] void should_link_pi_agents_at_the_generated_adapters() => AgentsTarget.ShouldEqual("../.cratis/ai/harnesses/pi/agents");
    [Fact] void should_load_the_agent_with_pi_native_tools() => ToolsOf(Path.Combine(_project, ".pi", "agents", "screenplay-modeler.md")).ShouldEqual("read, grep, find, bash, edit, write");
    [Fact] void should_still_install_the_canonical_agent() => File.Exists(Path.Combine(_project, ".cratis", "ai", "agents", "screenplay-modeler.md")).ShouldBeTrue();
    [Fact] void should_record_the_target_in_the_manifest() => InstalledManifest.ShouldContain("../.cratis/ai/harnesses/pi/agents");
    [Fact] void should_report_a_clean_status() => AiCorpusSynchronizer.Status(_project).ModifiedFiles.ShouldBeEmpty();
    [Fact] void should_have_no_conflicts() => _result.Conflicts.ShouldBeEmpty();
}
