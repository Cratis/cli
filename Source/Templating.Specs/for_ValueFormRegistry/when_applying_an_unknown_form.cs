// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Specs.for_ValueFormRegistry.given;
using Cratis.Templating.ValueForms;

namespace Cratis.Templating.Specs.for_ValueFormRegistry;

public class when_applying_an_unknown_form : a_registry
{
    [Fact] void should_fail_loudly() =>
        Catch.Exception(() => BuiltIns.Apply("no_such_form", "x")).ShouldNotBeNull();
}
