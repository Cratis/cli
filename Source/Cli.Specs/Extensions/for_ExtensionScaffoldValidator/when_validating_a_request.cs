// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Scaffolding;

namespace Cratis.Cli.Extensions.for_ExtensionScaffoldValidator;

public class when_validating_a_request : Specification
{
    static IReadOnlyList<string> Errors(ExtensionScaffoldRequest request) => ExtensionScaffoldValidator.Validate(request).Errors;

    [Fact] void should_accept_every_backend_and_frontend_combination_of_a_full_extension() =>
        ExtensionScaffoldVocabulary.Backends.Append("none").SelectMany(backend => ExtensionScaffoldVocabulary.Frontends.Append("none").Select(frontend => (backend, frontend)))
            .All(pair => Errors(new("Stripe", ExtensionScaffoldKind.Extension, pair.backend, pair.frontend)).Count == 0).ShouldBeTrue();

    [Fact] void should_accept_every_component_of_a_frontend_only_package() =>
        ExtensionScaffoldVocabulary.Components.All(component => Errors(new("Form", ExtensionScaffoldKind.Package, Component: component)).Count == 0).ShouldBeTrue();

    [Fact] void should_ignore_the_case_of_a_choice() => ExtensionScaffoldValidator.Validate(new("Stripe", ExtensionScaffoldKind.Extension, "CSharp", "React")).Request.Backend.ShouldEqual("csharp");
    [Fact] void should_reject_a_name_that_is_not_pascal_case() => Errors(new("stripe-payments", ExtensionScaffoldKind.Extension)).ShouldContain(error => error.Contains("PascalCase"));
    [Fact] void should_reject_an_empty_name() => Errors(new(string.Empty, ExtensionScaffoldKind.Extension)).Count.ShouldEqual(1);
    [Fact] void should_reject_an_unknown_backend() => Errors(new("Stripe", ExtensionScaffoldKind.Extension, "cobol")).ShouldContain(error => error.Contains("Unknown backend 'cobol'"));
    [Fact] void should_reject_an_unknown_frontend() => Errors(new("Stripe", ExtensionScaffoldKind.Extension, Frontend: "vue")).ShouldContain(error => error.Contains("Unknown frontend 'vue'"));
    [Fact] void should_reject_a_component_on_a_full_extension() => Errors(new("Stripe", ExtensionScaffoldKind.Extension, Component: "react")).ShouldContain(error => error.Contains("--component"));
    [Fact] void should_require_a_component_for_a_frontend_only_package() => Errors(new("Form", ExtensionScaffoldKind.Package)).ShouldContain(error => error.Contains("needs a --component"));
    [Fact] void should_reject_a_backend_on_a_frontend_only_package() => Errors(new("Form", ExtensionScaffoldKind.Package, "go", Component: "react")).ShouldContain(error => error.Contains("no backend"));
    [Fact] void should_reject_a_screenplay_component_on_a_frontend_only_package() => Errors(new("Form", ExtensionScaffoldKind.Package, Component: "screenplay")).ShouldContain(error => error.Contains("Unknown component"));
    [Fact] void should_spell_a_name_out_as_words() => ExtensionScaffoldValidator.SpellOut("OrderTracking").ShouldEqual("Order Tracking");
}
