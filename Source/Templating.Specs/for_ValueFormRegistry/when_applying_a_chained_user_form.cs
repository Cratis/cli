// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Specs.for_ValueFormRegistry.given;
using Cratis.Templating.ValueForms;

namespace Cratis.Templating.Specs.for_ValueFormRegistry;

public class when_applying_a_chained_user_form : a_registry
{
    [Fact] void should_apply_steps_in_order()
    {
        var registry = Registry(
            ("dotToUnderscore", "replace", "\\.", "_", null),
            ("shout", "upperCaseInvariant", null, null, null),
            ("chained", "chain", null, null, ["dotToUnderscore", "shout"]));

        registry.Apply("chained", "a.b").ShouldEqual("A_B");
    }
}
