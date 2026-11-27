// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_a_retired_link_is_already_missing : given.an_installation_with_agents_and_prompts
{
    const string MissingLink = ".pi/prompts/example.md";

    void Establish()
    {
        File.Delete(Path.Combine(_project, MissingLink));
        RetireAgentsPromptsAndTheScanHook();
    }

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, AllHarnesses);

    [Fact] void should_update_without_conflicts() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_keep_the_preexisting_empty_directory() => Directory.Exists(Path.Combine(_project, ".pi", "prompts")).ShouldBeTrue();
    [Fact] void should_not_report_an_action_for_the_missing_link() => _result.Actions.ShouldNotContain(action => action.EndsWith($" {MissingLink}", StringComparison.Ordinal));
}
