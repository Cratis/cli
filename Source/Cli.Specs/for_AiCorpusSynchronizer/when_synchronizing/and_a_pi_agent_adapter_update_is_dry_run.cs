// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_a_pi_agent_adapter_update_is_dry_run : given.a_pi_corpus
{
    void Establish()
    {
        AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);
        AddAdapter();
    }

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration, dryRun: true);

    [Fact] void should_report_the_relink() => _result.Actions.ShouldContain("Configured .pi/agents");
    [Fact] void should_keep_the_canonical_agents_link() => AgentsTarget.ShouldEqual("../.cratis/ai/agents");
    [Fact] void should_not_install_the_adapter() => File.Exists(Path.Combine(_project, ".cratis", "ai", "harnesses", "pi", "agents", "screenplay-modeler.md")).ShouldBeFalse();
    [Fact] void should_keep_the_previous_target_in_the_manifest() => InstalledManifest.ShouldNotContain("harnesses/pi/agents");
}
