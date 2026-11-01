// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_uninstalling;

public class and_pi_agent_adapters_are_installed : given.a_pi_corpus
{
    void Establish()
    {
        AddAdapter();
        AiCorpusSynchronizer.Synchronize(_project, _corpus, Configuration);
    }

    void Because() => _result = AiCorpusSynchronizer.Uninstall(_project);

    [Fact] void should_remove_the_agents_link() => Path.Exists(Path.Combine(_project, ".pi", "agents")).ShouldBeFalse();
    [Fact] void should_remove_the_adapter() => File.Exists(Path.Combine(_project, ".cratis", "ai", "harnesses", "pi", "agents", "screenplay-modeler.md")).ShouldBeFalse();
    [Fact] void should_have_no_conflicts() => _result.Conflicts.ShouldBeEmpty();
}
