// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_the_canonical_negated_policy : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;
    string _path = null!;

    void Establish()
    {
        _path = Path.Combine(_folder, "canonical-negation");
        foreach (var document in PolicyNegationCorpus.V7.SourceForms.Single(form => form.Name == "folder").Documents)
        {
            var path = Path.Combine(_path, document.DisplayPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, document.Bytes.AsSpan());
        }
    }

    async Task Because() => _result = await Plan(_path);

    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_no_errors() => _result.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact]
    void should_warn_that_the_unauthenticated_scenario_was_skipped() =>
        _result.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning)
            .Select(diagnostic => diagnostic.Message).ShouldContain("Skipped specification 'UnauthenticatedDenied' (STAGE-ESM-011): Specification 'UnauthenticatedDenied' cannot render: An unauthenticated caller cannot carry roles or claims; Arc supplies an empty guest principal.");
    [Fact] void should_negate_the_service_role() => Policies().ShouldContain("PolicyValues.Not(context.Principal.IsInRole(\"Service\"))");
    [Fact] void should_negate_the_service_claim() => Policies().ShouldContain("PolicyValues.Not(PolicyValues.Truth(context, \"actorKind\", \"service\"))");
    [Fact] void should_keep_unknown_unknown_under_negation() => Policies().ShouldContain("public static bool? Not(bool? value) => value is null ? null : !value.Value;");
    [Fact]
    void should_not_generate_the_unsupported_unauthenticated_scenario() =>
        _result.Artifacts!.Artifacts.Where(artifact => artifact.RelativePath.EndsWith("when_unauthenticated_denied.cs", StringComparison.Ordinal)).ShouldBeEmpty();

    /// <summary>Stage renders the helper in Policies.cs and each policy expression in its own GeneratedPolicies file.</summary>
    /// <returns>The combined generated policy source.</returns>
    string Policies() => string.Join(
        '\n',
        _result.Artifacts!.Artifacts
            .Where(artifact => artifact.RelativePath.Replace('\\', '/').StartsWith("GeneratedPolicies/", StringComparison.Ordinal))
            .Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
}
