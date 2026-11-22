// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildPrompts;

public class when_composing_the_prompt_for_a_build_that_also_cleans_up : Specification
{
    string _prompt;

    void Because()
    {
        var json = JsonNode.Parse(given.an_order.Json())!.AsObject();
        json["changes"] = new JsonArray(new JsonObject
        {
            ["sliceId"] = given.an_order.GoneSliceId,
            ["kind"] = "removed",
            ["declarations"] = new JsonArray("CancelInvoice"),
            ["previousName"] = "CancelInvoice"
        });
        var order = BuildOrder.Parse(json.ToJsonString());
        _prompt = BuildPrompts.ForOrder(null, BuildKitPlaceholders.For(order, "order.json", "outcome.json", string.Empty), order);
    }

    [Fact] void should_still_build() => _prompt.ShouldNotContain("This order only removes.");
    [Fact] void should_say_which_slices_to_clean_up() => _prompt.ShouldContain($"- slice {given.an_order.GoneSliceId}; former name: CancelInvoice; declarations: CancelInvoice");
    [Fact] void should_say_names_are_not_paths() => _prompt.ShouldContain("names, not file paths");
}
