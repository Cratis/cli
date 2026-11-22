// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildPrompts;

public class when_composing_the_prompt_for_a_removal_order : Specification
{
    string _prompt;
    string _withKit;

    void Because()
    {
        var order = given.an_order.ParseRemoval();
        var values = BuildKitPlaceholders.For(order, "order.json", "outcome.json", string.Empty);
        _prompt = BuildPrompts.ForOrder(null, values, order);
        _withKit = BuildPrompts.ForOrder("Build {slice} now.", values, order);
    }

    [Fact] void should_say_the_order_only_removes() => _prompt.ShouldContain("This order only removes.");
    [Fact] void should_list_the_slice_id() => _prompt.ShouldContain(given.an_order.GoneSliceId);
    [Fact] void should_list_the_declaration_names() => _prompt.ShouldContain("declarations: CancelInvoice, InvoiceCancelled");
    [Fact] void should_say_names_are_not_paths() => _prompt.ShouldContain("names, not file paths");
    [Fact] void should_keep_the_removal_instructions_with_a_kit_template() => _withKit.ShouldContain("This order only removes.");
    [Fact] void should_fill_the_kit_template_with_the_former_name() => _withKit.ShouldContain("Build CancelInvoice now.");
    [Fact] void should_say_the_kit_does_not_change_that_it_only_removes() => _withKit.ShouldContain("do not change that this order only removes");
    [Fact] void should_end_with_the_outcome_rules() => _prompt.ShouldContain("write the outcome as JSON to outcome.json");
    [Fact] void should_ask_for_the_removed_files_in_the_outcome() => _prompt.ShouldContain("files lists every path you deleted");
}
