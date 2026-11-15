// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildPrompts;

public class when_composing_the_prompt_for_an_order_with_instructions : Specification
{
    string _prompt;

    void Because()
    {
        var order = BuildOrder.Parse(given.an_order.WithInstructions());
        _prompt = BuildPrompts.ForOrder(null, BuildKitPlaceholders.For(order, "order.json", "outcome.json", string.Empty), order);
    }

    [Fact] void should_spell_out_the_instructions_from_the_model() => _prompt.ShouldContain("Instructions from the model:");
    [Fact] void should_list_the_slice_description() => _prompt.ShouldContain($"- {given.an_order.SliceDescription}");
    [Fact] void should_list_the_feedback_by_slice() => _prompt.ShouldContain($"- PayInvoice: {given.an_order.FeedbackText}");
    [Fact] void should_list_the_specifications_as_acceptance_criteria() => _prompt.ShouldContain($"- PayInvoice: Paying an open invoice ({given.an_order.SpecificationId})");
    [Fact] void should_point_at_the_hints_and_feedback_in_the_order() => _prompt.ShouldContain("context.hints");
    [Fact] void should_end_with_the_outcome_rules() => _prompt.ShouldContain("write the outcome as JSON to outcome.json");
}
