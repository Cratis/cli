// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CompletionsCommand;

public class when_completing_offline_values : Specification
{
    [Fact] void should_recognize_all_local_contexts() => new[] { "output-formats", "contexts", "ai-profiles", "ai-harnesses", "ai-languages", "llm-kinds", "llm-models" }
        .All(OfflineCompletion.IsStaticContext).ShouldBeTrue();
    [Fact] void should_leave_chronicle_contexts_dynamic() => OfflineCompletion.IsStaticContext("event-types").ShouldBeFalse();
    [Fact] void should_complete_output_formats_without_a_server() => OfflineCompletion.Candidates("output-formats", "j", "/unused").ShouldContain("json");
    [Fact] void should_suggest_the_known_provider_kinds() => OfflineCompletion.Candidates("llm-kinds", "", "/unused").ShouldContainOnly(LlmKinds.All);
    [Fact] void should_keep_selected_harnesses() => OfflineCompletion.Candidates("ai-harnesses", "claude,p", "/unused").ShouldContain("claude,pi");
    [Fact] void should_not_repeat_selected_harnesses() => OfflineCompletion.Candidates("ai-harnesses", "pi,", "/unused").ShouldNotContain("pi,pi");
    [Fact] void should_keep_selected_profiles() => OfflineCompletion.Candidates("ai-profiles", "cratis/documentation,cratis/eng", "/unused")
        .ShouldContain("cratis/documentation,cratis/engineering/csharp");
    [Fact] void should_suggest_screenplay_before_ai_is_installed() => OfflineCompletion.Candidates("ai-profiles", "cratis/scr", "/unused")
        .ShouldContain("cratis/screenplay");
    [Fact] void should_suggest_only_framework_model_defaults_and_safe_local_hints() => OfflineCompletion.Candidates("llm-models", "", "/unused")
        .ShouldContain(LlmKinds.DefaultModelFor(LlmKinds.Anthropic));
    [Fact] void should_reject_shell_control_characters_in_current_word() => OfflineCompletion.Candidates("ai-harnesses", "pi;$(touch /tmp/unsafe),", "/unused").ShouldBeEmpty();
}
