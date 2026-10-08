// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_plan_no_publishable_artifacts() => _result.Artifacts!.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_the_renderer_refusal() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldEqual("STAGE-ESM-011");
    [Fact] void should_name_the_unauthenticated_scenario() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-011").Message.ShouldContain("UnauthenticatedDenied");
    [Fact] void should_explain_the_guest_principal_restriction() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-011").Message.ShouldContain("An unauthenticated caller cannot carry roles or claims");
}
