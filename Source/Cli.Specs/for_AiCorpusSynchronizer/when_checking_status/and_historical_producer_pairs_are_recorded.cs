// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_checking_status;

public class and_historical_producer_pairs_are_recorded : Specification
{
    HashSet<AiManagedIntegration> _withAdapters = null!;
    HashSet<AiManagedIntegration> _withoutAdapters = null!;

    void Because()
    {
        _withAdapters = AiHarnessIntegrations.Registry(["harnesses/pi/agents/modeler.md", "harnesses/opencode/agents/modeler.md", "harnesses/copilot/instructions/general.instructions.md"]);
        _withoutAdapters = AiHarnessIntegrations.Registry([]);
    }

    [Fact] void should_accept_canonical_pi_agents_with_adapters() => _withAdapters.Contains(new(".pi/agents", "../.cratis/ai/agents", true)).ShouldBeTrue();
    [Fact] void should_accept_canonical_opencode_agents_with_adapters() => _withAdapters.Contains(new(".opencode/agents", "../.cratis/ai/agents", true)).ShouldBeTrue();
    [Fact] void should_accept_canonical_copilot_rules_with_adapters() => _withAdapters.Contains(new(".github/instructions", "../.cratis/ai/rules", true)).ShouldBeTrue();
    [Fact] void should_accept_pi_adapters_without_recorded_adapter_files() => _withoutAdapters.Contains(new(".pi/agents", "../.cratis/ai/harnesses/pi/agents", true)).ShouldBeTrue();
    [Fact] void should_accept_opencode_adapters_without_recorded_adapter_files() => _withoutAdapters.Contains(new(".opencode/agents", "../.cratis/ai/harnesses/opencode/agents", true)).ShouldBeTrue();
    [Fact] void should_accept_copilot_adapters_without_recorded_adapter_files() => _withoutAdapters.Contains(new(".github/instructions", "../.cratis/ai/harnesses/copilot/instructions", true)).ShouldBeTrue();
    [Fact] void should_accept_the_old_nested_claude_general_record() => _withoutAdapters.Contains(new(".claude/CLAUDE.md", "../.cratis/ai/rules/general.md", false)).ShouldBeTrue();
    [Fact] void should_accept_the_old_nested_claude_project_record() => _withoutAdapters.Contains(new(".claude/CLAUDE.md", "../.cratis/ai/rules/project.md", false)).ShouldBeTrue();
}
