// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Scaffolding;
using Cratis.Templating;

namespace Cratis.Cli.Extensions.for_ExtensionScaffolder;

public class when_creating_a_frontend_only_package : Specification
{
    string _output = null!;
    ExtensionScaffoldResult _result = null!;

    async Task Because()
    {
        var templates = ExtensionTemplateFixture.Create();
        _output = Path.Combine(Path.GetTempPath(), "cratis-specs-extension-out", Guid.NewGuid().ToString("N"));
        _result = await new ExtensionScaffolder(new TemplatingEngine(Path.Combine(templates, "store")))
            .Create(new("PaymentForm", ExtensionScaffoldKind.Package, Component: "mount"), new(Output: _output, TemplatePath: templates), CancellationToken.None);
    }

    [Fact] void should_create_the_package() => _result.Succeeded.ShouldBeTrue();
    [Fact] void should_use_the_package_template_with_the_component() => File.ReadAllText(Path.Combine(_output, "contract.play")).ShouldContain("component mount");
}
