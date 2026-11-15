// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildPrompts;

public class when_composing_the_prompt_for_an_order_without_instructions : Specification
{
    BuildOrder _order;

    void Establish() => _order = BuildOrder.Parse(given.an_order.Json().Replace("\"specifications\":[", "\"ignored\":[", StringComparison.Ordinal));

    [Fact] void should_add_no_instructions_section() => BuildPrompts.ModelInstructions(_order).ShouldEqual(string.Empty);
}
