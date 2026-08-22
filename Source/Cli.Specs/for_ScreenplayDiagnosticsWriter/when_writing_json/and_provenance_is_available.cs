// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ScreenplayDiagnosticsWriter.when_writing_json;

public class and_provenance_is_available : Specification
{
    JsonElement _result;

    void Because()
    {
        var json = ScreenplayDiagnosticsWriter.JsonFor(
            OutputFormats.JsonCompact,
            [],
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
                ],
                new ScreenplayCompatibilityReport(
                    ScreenplaySupportTier.Canonical,
                    ScreenplayRecognitionStatus.Recognized,
                    ScreenplaySemanticConformance.RequiresHumanReview,
                    ScreenplayLoweringFidelity.NoReportedLoss,
                    "canonical fixture evidence")));
        _result = JsonDocument.Parse(json).RootElement.Clone();
    }

    [Fact] void should_report_the_provider_version() => _result.GetProperty("provenance").GetProperty("providerVersion").GetString().ShouldEqual("0.3.0");
    [Fact] void should_report_the_selected_target_framework() => _result.GetProperty("provenance").GetProperty("projects")[0].GetProperty("targetFramework").GetString().ShouldEqual("net9.0");
    [Fact] void should_report_the_resolved_package_version() => _result.GetProperty("provenance").GetProperty("projects")[0].GetProperty("packages")[0].GetProperty("version").GetString().ShouldEqual("9.23.0");
    [Fact] void should_report_the_assembly_version_separately() => _result.GetProperty("provenance").GetProperty("projects")[0].GetProperty("assemblies")[0].GetProperty("version").GetString().ShouldEqual("9.0.0.0");
    [Fact] void should_report_the_support_tier() => _result.GetProperty("provenance").GetProperty("compatibility").GetProperty("supportTier").GetString().ShouldEqual("Canonical");
    [Fact] void should_report_semantic_conformance_separately() => _result.GetProperty("provenance").GetProperty("compatibility").GetProperty("semanticConformance").GetString().ShouldEqual("RequiresHumanReview");
    [Fact] void should_report_lowering_fidelity_separately() => _result.GetProperty("provenance").GetProperty("compatibility").GetProperty("loweringFidelity").GetString().ShouldEqual("NoReportedLoss");
}
