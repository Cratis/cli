// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Scaffolding;
using Cratis.Templating;

namespace Cratis.Cli.Extensions.for_ExtensionScaffolder;

public class when_creating_an_extension : Specification
{
    string _templates = null!;
    string _output = null!;
    ExtensionScaffoldResult _result = null!;

    void Establish()
    {
        _templates = ExtensionTemplateFixture.Create();
        _output = Path.Combine(Path.GetTempPath(), "cratis-specs-extension-out", Guid.NewGuid().ToString("N"));
    }

    async Task Because() => _result = await new ExtensionScaffolder(new TemplatingEngine(Path.Combine(_templates, "store")))
        .Create(
            new("Stripe", ExtensionScaffoldKind.Extension, "go", "react", DisplayName: "Stripe payments", Description: "Card payments", Publisher: "acme"),
            new(Output: _output, TemplatePath: _templates),
            CancellationToken.None);

    string Contract => File.ReadAllText(Path.Combine(_output, "contract.play"));

    [Fact] void should_create_the_package() => _result.Succeeded.ShouldBeTrue();
    [Fact] void should_name_the_package_after_the_request() => _result.Creation!.Name.ShouldEqual("Stripe");
    [Fact] void should_pass_the_display_name() => Contract.ShouldContain("display name \"Stripe payments\"");
    [Fact] void should_pass_the_description() => Contract.ShouldContain("description \"Card payments\"");
    [Fact] void should_pass_the_backend() => Contract.ShouldContain("backend go");
    [Fact] void should_pass_the_frontend() => Contract.ShouldContain("frontend react");
    [Fact] void should_pass_the_publisher() => Contract.ShouldContain("publisher acme");
    [Fact] void should_name_the_extension_after_the_package() => Contract.ShouldContain("extension Stripe");
}
