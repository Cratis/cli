// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Scaffolding;
using Cratis.Templating;

namespace Cratis.Cli.Extensions.for_ExtensionScaffolder;

public class when_the_template_cannot_serve_the_request : Specification
{
    string _templates = null!;
    ExtensionScaffoldResult _missing = null!;
    ExtensionScaffoldResult _unsupported = null!;

    async Task Because()
    {
        _templates = ExtensionTemplateFixture.Create();
        Directory.Delete(Path.Combine(_templates, "package"), true);
        var scaffolder = new ExtensionScaffolder(new TemplatingEngine(Path.Combine(_templates, "store")));
        var output = Path.Combine(Path.GetTempPath(), "cratis-specs-extension-out", Guid.NewGuid().ToString("N"));
        _missing = await scaffolder.Create(new("PaymentForm", ExtensionScaffoldKind.Package, Component: "react"), new(Output: output, TemplatePath: _templates), CancellationToken.None);
        _unsupported = await scaffolder.Create(new("Stripe", ExtensionScaffoldKind.Extension, "cobol"), new(Output: output, TemplatePath: _templates), CancellationToken.None);
    }

    [Fact] void should_fail_when_the_template_is_missing() => _missing.Succeeded.ShouldBeFalse();
    [Fact] void should_name_the_missing_template() => _missing.Error.ShouldContain(ExtensionTemplateInterface.PackageShortName);
    [Fact] void should_fail_when_the_template_does_not_offer_a_choice() => _unsupported.Succeeded.ShouldBeFalse();
    [Fact] void should_say_which_choice_is_not_offered() => _unsupported.Error.ShouldContain("does not offer 'cobol'");
}
