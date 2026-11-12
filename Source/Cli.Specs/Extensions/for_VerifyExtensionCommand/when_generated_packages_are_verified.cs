// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Scaffolding;
using Cratis.Cli.Extensions.Verification;
using Cratis.Templating;

namespace Cratis.Cli.Extensions.for_VerifyExtensionCommand;

public class when_generated_packages_are_verified : Specification
{
    readonly List<(string Name, ExtensionVerifyReport Report)> _reports = [];

    async Task Because()
    {
        var templates = ExtensionTemplateFixture.Create();
        var scaffolder = new ExtensionScaffolder(new TemplatingEngine(Path.Combine(templates, "store")));
        var verifier = new ExtensionVerifier(
            new UnavailableRuntimeCheck(ExtensionVerifyCodes.FrontendCheck, ExtensionVerifyCodes.FrontendNotRun, "not run"),
            new UnavailableRuntimeCheck(ExtensionVerifyCodes.BackendCheck, ExtensionVerifyCodes.BackendNotRun, "not run"));
        var requests = ExtensionScaffoldVocabulary.Backends.Append("none")
            .SelectMany(backend => ExtensionScaffoldVocabulary.Frontends.Append("none").Select(frontend => new ExtensionScaffoldRequest("Demo", ExtensionScaffoldKind.Extension, backend, frontend)))
            .Concat(ExtensionScaffoldVocabulary.Components.Select(component => new ExtensionScaffoldRequest("Demo", ExtensionScaffoldKind.Package, Component: component)));
        foreach (var request in requests)
        {
            var output = Path.Combine(Path.GetTempPath(), "cratis-specs-extension-generated", Guid.NewGuid().ToString("N"));
            var result = await scaffolder.Create(request, new(Output: output, TemplatePath: templates), CancellationToken.None);
            _reports.Add(($"{request.Kind} {request.Backend} {request.Frontend} {request.Component}", result.Succeeded ? await verifier.Verify("generated", output, new(CompilerVersion: "6.22.0"), CancellationToken.None) : new("generated", [new("contract", VerifyStatus.Failed, [new("contract", "CREATE", VerifySeverity.Error, result.Error!)])])));
        }
    }

    [Fact] void should_create_and_verify_every_combination() => _reports.Count.ShouldEqual(22);
    [Fact] void should_pass_every_generated_package() => _reports.Where(item => !item.Report.Passed).Select(item => item.Name + ": " + string.Join("; ", item.Report.Diagnostics.Select(diagnostic => diagnostic.Code + " " + diagnostic.Message))).ShouldBeEmpty();
}
