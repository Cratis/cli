// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_the_claude_settings_are_user_owned_when_a_hook_is_retired : given.an_installation_with_agents_and_prompts
{
    const string Own = """{"hooks":[{"command":"bash my-own-hook.sh"}]}""";

    void Establish()
    {
        File.Delete(ClaudeSettings);
        File.WriteAllText(ClaudeSettings, Own);
        RetireAgentsPromptsAndTheScanHook();
    }

    void Because() => _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, AllHarnesses);

    [Fact] void should_keep_the_users_settings() => File.ReadAllText(ClaudeSettings).ShouldEqual(Own);
}
