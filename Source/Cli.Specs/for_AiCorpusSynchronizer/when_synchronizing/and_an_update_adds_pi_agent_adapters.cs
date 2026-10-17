// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_an_update_adds_pi_agent_adapters : given.a_pi_corpus
{
    string _previousTarget = null!;
    string _unrelated = null!;
    byte[] _canonicalBefore = null!;

    void Establish()
    {
        AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);
        _previousTarget = AgentsTarget;
        _canonicalBefore = File.ReadAllBytes(CanonicalInstalled);
        _unrelated = Path.Combine(_project, ".pi", "settings.json");
        File.WriteAllText(_unrelated, "{}");
        AddAdapter();
    }

    string CanonicalInstalled => Path.Combine(_project, ".cratis", "ai", "agents", "screenplay-modeler.md");

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);

    [Fact] void should_start_with_the_canonical_agents_link() => _previousTarget.ShouldEqual("../.cratis/ai/agents");
    [Fact] void should_relink_the_owned_agents() => AgentsTarget.ShouldEqual("../.cratis/ai/harnesses/pi/agents");
    [Fact] void should_record_the_relink() => _result.Actions.ShouldContain("Configured .pi/agents");
    [Fact] void should_keep_the_canonical_agent_bytes() => File.ReadAllBytes(CanonicalInstalled).ShouldEqual(_canonicalBefore);
    [Fact] void should_keep_unrelated_files() => File.ReadAllText(_unrelated).ShouldEqual("{}");
    [Fact] void should_record_the_new_target_in_the_manifest() => InstalledManifest.ShouldContain("../.cratis/ai/harnesses/pi/agents");
    [Fact] void should_not_report_owned_link_drift() => AiCorpusSynchronizer.Status(_project).ModifiedFiles.ShouldBeEmpty();
    [Fact] void should_have_no_conflicts() => _result.Conflicts.ShouldBeEmpty();
}
