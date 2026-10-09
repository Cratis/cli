// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_a_negated_policy : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;
    string _path = null!;

    void Establish()
    {
        _path = Path.Combine(_folder, "negation");
        foreach (var document in PolicyNegationCorpus.V7.SourceForms.Single(_ => _.Name == "folder").Documents)
        {
            var path = Path.Combine(_path, document.DisplayPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            // Arc gives an unauthenticated caller an empty principal; keep the corpus's denial assertion.
            var source = document.Text.Replace(
                "      specification UnauthenticatedDenied\n        given caller\n          role \"Accountant\"\n",
                "      specification UnauthenticatedDenied\n        given caller\n",
                StringComparison.Ordinal);
            File.WriteAllText(path, source);
        }
    }

    async Task Because() => _result = await Plan(_path);

    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_plan_publishable_artifacts() => _result.Artifacts!.Artifacts.ShouldNotBeEmpty();
    [Fact] void should_report_no_errors() => _result.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_negate_the_service_role() => Policies().ShouldContain("PolicyValues.Not(context.Principal.IsInRole(\"Service\"))");
    [Fact] void should_negate_the_service_claim() => Policies().ShouldContain("PolicyValues.Not(PolicyValues.Truth(context, \"actorKind\", \"service\"))");
    [Fact] void should_keep_unknown_unknown_under_negation() => Policies().ShouldContain("public static bool? Not(bool? value) => value is null ? null : !value.Value;");

    // Stage renders the helper in Policies.cs and each policy expression in its own GeneratedPolicies file.
    string Policies() => string.Join(
        "\n",
        _result.Artifacts!.Artifacts
            .Where(artifact => artifact.RelativePath.Replace('\\', '/').StartsWith("GeneratedPolicies/", StringComparison.Ordinal))
            .Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
}
