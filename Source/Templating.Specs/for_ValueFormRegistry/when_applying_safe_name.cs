// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Specs.for_ValueFormRegistry.given;
using Cratis.Templating.ValueForms;

namespace Cratis.Templating.Specs.for_ValueFormRegistry;

public class when_applying_safe_name : a_registry
{
    [Fact] void should_transform_the_documented_class_name_example() => BuiltIns.Apply("safe_name", "Template.1").ShouldEqual("Template__1");
    [Fact] void should_replace_non_word_characters() => BuiltIns.Apply("safe_name", "My-App").ShouldEqual("My_App");
    [Fact] void should_prefix_a_leading_digit_segment() => BuiltIns.Apply("safe_name", "1Thing").ShouldEqual("_1Thing");
}
