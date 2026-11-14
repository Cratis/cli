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
        var verifier = RuntimeChecks.CreateVerifier();
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
    IEnumerable<VerifyStatus> RuntimeCheckStatuses => _reports.SelectMany(item => item.Report.Checks).Where(check => string.Equals(check.Check, ExtensionVerifyCodes.FrontendCheck, StringComparison.Ordinal) || string.Equals(check.Check, ExtensionVerifyCodes.BackendCheck, StringComparison.Ordinal)).Select(check => check.Status);

    [Fact] void should_skip_the_frontend_and_backend_checks_wherever_they_apply() => RuntimeCheckStatuses.Distinct().Order().ShouldEqual([VerifyStatus.Skipped, VerifyStatus.NotApplicable]);
    [Fact] void should_not_run_any_runtime_check_to_a_pass() => RuntimeCheckStatuses.ShouldNotContain(VerifyStatus.Passed);
    [Fact] void should_pass_every_generated_package_non_strictly() => _reports.Count(item => item.Report.Passed).ShouldEqual(22);
    [Fact] void should_pass_every_generated_package() => _reports.Where(item => !item.Report.Passed).Select(item => item.Name + ": " + string.Join("; ", item.Report.Diagnostics.Select(diagnostic => diagnostic.Code + " " + diagnostic.Message))).ShouldBeEmpty();
}
