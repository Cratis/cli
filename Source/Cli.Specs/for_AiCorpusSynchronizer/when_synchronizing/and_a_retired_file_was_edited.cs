// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_a_retired_file_was_edited : given.an_installation_with_agents_and_prompts
{
    string _edited = null!;

    void Establish()
    {
        _edited = Path.Combine(_project, ".cratis", "ai", "agents", "screenplay-modeler.md");
        File.WriteAllText(_edited, "# My own agent");
        RetireAgentsPromptsAndTheScanHook();
    }

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, AllHarnesses);

    [Fact] void should_report_the_edited_file() => _result.Conflicts.ShouldContain("agents/screenplay-modeler.md");
    [Fact] void should_keep_the_edit() => File.ReadAllText(_edited).ShouldEqual("# My own agent");
    [Fact] void should_change_nothing_else() => RetiredLinks.Where(link => !Occupied(link)).ShouldBeEmpty();
}
