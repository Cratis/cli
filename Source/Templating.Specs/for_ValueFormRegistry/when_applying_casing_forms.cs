// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Specs.for_ValueFormRegistry.given;
using Cratis.Templating.ValueForms;

namespace Cratis.Templating.Specs.for_ValueFormRegistry;

public class when_applying_casing_forms : a_registry
{
    [Fact] void should_kebab_case() => BuiltIns.Apply("kebabCase", "Company.Crisis").ShouldEqual("company-crisis");
    [Fact] void should_snake_case() => BuiltIns.Apply("snakeCase", "Company Crisis").ShouldEqual("company_crisis");
    [Fact] void should_first_upper_case() => BuiltIns.Apply("firstUpperCase", "myApp").ShouldEqual("MyApp");
    [Fact] void should_upper_case_invariant() => BuiltIns.Apply("upperCaseInvariant", "mixed Case").ShouldEqual("MIXED CASE");
}
