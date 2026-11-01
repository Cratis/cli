// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_the_corpus_has_no_pi_agent_adapters : given.a_pi_corpus
{
    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);

    [Fact] void should_link_pi_agents_at_the_canonical_agents() => AgentsTarget.ShouldEqual("../.cratis/ai/agents");
    [Fact] void should_have_no_conflicts() => _result.Conflicts.ShouldBeEmpty();
}
