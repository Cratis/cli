// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Specs.for_ValueFormRegistry.given;
using Cratis.Templating.ValueForms;

namespace Cratis.Templating.Specs.for_ValueFormRegistry;

public class when_applying_safe_namespace : a_registry
{
    [Fact] void should_transform_the_documented_namespace_example() => BuiltIns.Apply("safe_namespace", "Template.1").ShouldEqual("Template._1");
    [Fact] void should_keep_dots_as_separators() => BuiltIns.Apply("safe_namespace", "Company.Web").ShouldEqual("Company.Web");
    [Fact] void should_replace_invalid_characters_within_segments() => BuiltIns.Apply("safe_namespace", "My-App.Web").ShouldEqual("My_App.Web");
}
