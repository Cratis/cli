// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Specs.for_ValueFormRegistry.given;
using Cratis.Templating.ValueForms;

namespace Cratis.Templating.Specs.for_ValueFormRegistry;

public class when_applying_lower_safe_forms : a_registry
{
    [Fact] void should_lower_safe_name() => BuiltIns.Apply("lower_safe_name", "My-App").ShouldEqual("my_app");
    [Fact] void should_lower_safe_namespace() => BuiltIns.Apply("lower_safe_namespace", "Template.1").ShouldEqual("template._1");
}
