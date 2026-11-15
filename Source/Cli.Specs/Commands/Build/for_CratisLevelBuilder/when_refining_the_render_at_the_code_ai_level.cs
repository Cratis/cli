// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_CratisLevelBuilder;

public class when_refining_the_render_at_the_code_ai_level : Specification
{
    string _prompt;
    string _axonPrompt;

    void Because()
    {
        var order = BuildOrder.Parse(given.an_order.WithInstructions());
        var hostPrompt = BuildPrompts.ForOrder(null, BuildKitPlaceholders.For(order, "order.json", "outcome.json", string.Empty), order);
        _prompt = CratisLevelBuilder.RefinePrompt(hostPrompt, "cratis");
        _axonPrompt = CratisLevelBuilder.RefinePrompt(hostPrompt, "axon");
    }

    [Fact] void should_say_the_render_is_already_done() => _prompt.ShouldContain("has already generated the application's structure");
    [Fact] void should_forbid_regenerating_or_editing_managed_files() => _prompt.ShouldContain("Do not regenerate it, and do not edit the files that carry a managed header");
    [Fact] void should_point_at_the_slice_descriptions_in_the_hints() => _prompt.ShouldContain("slice descriptions in the order's context.hints");
    [Fact] void should_point_at_the_described_handlers_and_queries() => _prompt.ShouldContain("command handlers and query descriptions");
    [Fact] void should_treat_specifications_as_acceptance_criteria() => _prompt.ShouldContain("specifications, each an acceptance criterion");
    [Fact] void should_point_at_the_feedback() => _prompt.ShouldContain("context.feedback");
    [Fact] void should_forbid_inventing_behavior() => _prompt.ShouldContain("Do not invent behavior the model does not describe");
    [Fact] void should_record_decisions_as_assumptions() => _prompt.ShouldContain("record it as an assumption in the outcome");
    [Fact] void should_carry_the_slice_description() => _prompt.ShouldContain(given.an_order.SliceDescription);
    [Fact] void should_carry_the_feedback() => _prompt.ShouldContain(given.an_order.FeedbackText);
    [Fact] void should_run_the_verification_commands() => _prompt.ShouldContain("run the verification commands");
    [Fact] void should_name_where_axon_lists_the_bodies_left() => _axonPrompt.ShouldContain("UNSUPPORTED.md and runtime/UnsupportedImplementations.java");
}
