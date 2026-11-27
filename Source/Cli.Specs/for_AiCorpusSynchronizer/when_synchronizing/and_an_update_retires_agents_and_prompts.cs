// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_an_update_retires_agents_and_prompts : given.an_installation_with_agents_and_prompts
{
    string[] _linksBefore = null!;
    string _ownCommand = null!;

    void Establish()
    {
        _linksBefore = [.. RecordedLinks.Select(link => link.Path)];
        _ownCommand = Path.Combine(_project, ".claude", "commands", "mine.md");
        File.WriteAllText(_ownCommand, "# Mine");
        RetireAgentsPromptsAndTheScanHook();
    }

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, AllHarnesses);

    [Fact] void should_have_installed_every_retired_link_before() => RetiredLinks.Except(_linksBefore).ShouldBeEmpty();
    [Fact] void should_not_report_a_conflict() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_remove_every_retired_link() => RetiredLinks.Where(Occupied).ShouldBeEmpty();
    [Fact] void should_remove_the_retired_files() => File.Exists(Path.Combine(_project, ".cratis", "ai", "agents", "screenplay-modeler.md")).ShouldBeFalse();
    [Fact] void should_record_no_link_without_a_target() => RecordedLinks.Where(link => !Path.Exists(Path.Combine(_project, link.Path))).ShouldBeEmpty();
    [Fact] void should_keep_the_links_to_what_remains() => new DirectoryInfo(Path.Combine(_project, ".claude", "rules")).LinkTarget.ShouldEqual("../.cratis/ai/rules");
    [Fact] void should_keep_the_file_the_user_added_beside_a_retired_link() => File.ReadAllText(_ownCommand).ShouldEqual("# Mine");
    [Fact] void should_drop_the_retired_hook_from_the_claude_settings() => File.ReadAllText(ClaudeSettings).ShouldNotContain(RetiredHook);
    [Fact] void should_keep_the_remaining_hook_in_the_claude_settings() => File.ReadAllText(ClaudeSettings).ShouldContain(GuardHook);
}
