// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Scaffolding;

namespace Cratis.Cli.Extensions.for_ExtensionScaffolder;

public class when_describing_the_template_interface : Specification
{
    IReadOnlyDictionary<string, string> _extension = null!;
    IReadOnlyDictionary<string, string> _package = null!;

    void Because()
    {
        _extension = ExtensionTemplateInterface.ParametersOf(new("OrderTracking", ExtensionScaffoldKind.Extension, "kotlin", "mount", Publisher: "acme"));
        _package = ExtensionTemplateInterface.ParametersOf(new("Form", ExtensionScaffoldKind.Package, Component: "react"));
    }

    [Fact] void should_select_the_extension_template_for_a_full_extension() => ExtensionTemplateInterface.ShortNameOf(ExtensionScaffoldKind.Extension).ShouldEqual("cratis-extension");
    [Fact] void should_select_the_package_template_for_a_frontend_only_package() => ExtensionTemplateInterface.ShortNameOf(ExtensionScaffoldKind.Package).ShouldEqual("cratis-extension-package");
    [Fact] void should_pass_the_backend_and_frontend_of_a_full_extension() => (_extension["Backend"], _extension["Frontend"]).ShouldEqual(("kotlin", "mount"));
    [Fact] void should_not_pass_a_component_for_a_full_extension() => _extension.ContainsKey("Component").ShouldBeFalse();
    [Fact] void should_pass_only_the_component_for_a_frontend_only_package() => (_package["Component"], _package.ContainsKey("Backend"), _package.ContainsKey("Frontend")).ShouldEqual(("react", false, false));
    [Fact] void should_spell_the_name_out_as_the_default_display_name() => _extension["DisplayName"].ShouldEqual("Order Tracking");
    [Fact] void should_pass_the_publisher() => _extension["Publisher"].ShouldEqual("acme");
    [Fact] void should_default_the_publisher() => _package["Publisher"].ShouldEqual("example");
}
