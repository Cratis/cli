// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ScreenplayDiagnosticsWriter.when_writing_json;

public class and_provenance_is_available : Specification
{
    string _json;
    JsonElement _result;

    void Because()
    {
        _json = ScreenplayDiagnosticsWriter.JsonFor(
            OutputFormats.JsonCompact,
            [
                new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, "DOTNETSP0013", "conflicting owners", "Source/Application.cs")
                {
                    Subject = "dotnet://Application/Orders.PlaceOrder",
                    Outcome = "Conflict"
                }
            ],
            new ScreenplayGenerationProvenance(
                ScreenplayProviders.CritterStack,
                "0.3.0",
                [
                    new ScreenplayProjectProvenance(
                        "Application",
                        "net9.0",
                        [new ResolvedScreenplayPackage("Marten", "9.23.0")],
                        [new ScreenplayAssemblyIdentity("Marten", "9.0.0.0")],
                        ["marten.event-projection"])
                    {
                        SourcePolicy = new ScreenplaySourcePolicyProvenance(
                            "Source/Application/Application.csproj",
                            "Source/Application/Application",
                            1,
                            "Workspace",
                            "Ordinal"),
                        SourceStructure = new ScreenplaySourceStructureProvenance(
                            "Application",
                            1,
                            "Features",
                            "Lending",
                            2)
                    }
                ],
                new ScreenplayCompatibilityReport(
                    ScreenplaySupportTier.Canonical,
                    ScreenplayRecognitionStatus.Recognized,
                    ScreenplaySemanticConformance.RequiresHumanReview,
                    ScreenplayLoweringFidelity.NoReportedLoss,
                    "canonical fixture evidence")));
        _result = JsonDocument.Parse(_json).RootElement.Clone();
    }

    [Fact] void should_report_the_provider_version() => _result.GetProperty("provenance").GetProperty("providerVersion").GetString().ShouldEqual("0.3.0");
    [Fact] void should_report_the_selected_target_framework() => _result.GetProperty("provenance").GetProperty("projects")[0].GetProperty("targetFramework").GetString().ShouldEqual("net9.0");
    [Fact] void should_report_the_resolved_package_version() => _result.GetProperty("provenance").GetProperty("projects")[0].GetProperty("packages")[0].GetProperty("version").GetString().ShouldEqual("9.23.0");
    [Fact] void should_report_the_assembly_version_separately() => _result.GetProperty("provenance").GetProperty("projects")[0].GetProperty("assemblies")[0].GetProperty("version").GetString().ShouldEqual("9.0.0.0");
    [Fact] void should_report_the_logical_project_path() => _result.GetProperty("provenance").GetProperty("projects")[0].GetProperty("sourcePolicy").GetProperty("logicalProjectPath").GetString().ShouldEqual("Source/Application/Application.csproj");
    [Fact] void should_report_the_stable_project_identity() => _result.GetProperty("provenance").GetProperty("projects")[0].GetProperty("sourcePolicy").GetProperty("projectIdentity").GetString().ShouldEqual("Source/Application/Application");
    [Fact] void should_report_the_source_policy_only() => _result.GetProperty("provenance").GetProperty("projects")[0].GetProperty("sourcePolicy").EnumerateObject().Select(_ => _.Name).ShouldContainOnly(["logicalProjectPath", "projectIdentity", "policyVersion", "displayRoot", "casePolicy"]);
    [Fact] void should_report_the_project_role() => _result.GetProperty("provenance").GetProperty("projects")[0].GetProperty("sourceStructure").GetProperty("projectRole").GetString().ShouldEqual("Application");
    [Fact] void should_report_the_source_structure_policy() => _result.GetProperty("provenance").GetProperty("projects")[0].GetProperty("sourceStructure").EnumerateObject().Select(_ => _.Name).ShouldContainOnly(["projectRole", "policyVersion", "featureRoot", "module", "namespaceSegmentsToSkip"]);
    [Fact] void should_report_the_feature_root() => _result.GetProperty("provenance").GetProperty("projects")[0].GetProperty("sourceStructure").GetProperty("featureRoot").GetString().ShouldEqual("Features");
    [Fact] void should_not_leak_a_physical_root() => _json.ShouldNotContain("/physical/");
    [Fact] void should_report_the_typed_diagnostic_subject() => _result.GetProperty("diagnostics")[0].GetProperty("subject").GetString().ShouldEqual("dotnet://Application/Orders.PlaceOrder");
    [Fact] void should_report_the_typed_diagnostic_outcome() => _result.GetProperty("diagnostics")[0].GetProperty("outcome").GetString().ShouldEqual("Conflict");
    [Fact] void should_report_the_support_tier() => _result.GetProperty("provenance").GetProperty("compatibility").GetProperty("supportTier").GetString().ShouldEqual("Canonical");
    [Fact] void should_report_semantic_conformance_separately() => _result.GetProperty("provenance").GetProperty("compatibility").GetProperty("semanticConformance").GetString().ShouldEqual("RequiresHumanReview");
    [Fact] void should_report_lowering_fidelity_separately() => _result.GetProperty("provenance").GetProperty("compatibility").GetProperty("loweringFidelity").GetString().ShouldEqual("NoReportedLoss");
}
