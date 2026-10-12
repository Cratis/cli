// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_RenderedSemanticVersions;

public class when_checking_admitted_versions : Specification
{
    ScreenplayDiagnostic? _newer;
    ScreenplayDiagnostic? _v10;
    ScreenplayDiagnostic? _v11;
    ScreenplayDiagnostic? _v12;
    ScreenplayDiagnostic? _v13;

    void Because()
    {
        _newer = RenderedSemanticVersions.Check(SemanticVersion.V14);
        _v10 = RenderedSemanticVersions.Check(SemanticVersion.V10);
        _v11 = RenderedSemanticVersions.Check(SemanticVersion.V11);
        _v12 = RenderedSemanticVersions.Check(SemanticVersion.V12);
        _v13 = RenderedSemanticVersions.Check(SemanticVersion.V13);
    }

    [Fact] void should_keep_v10_admitted() => _v10.ShouldBeNull();
    [Fact] void should_admit_v11_exact_numbers() => _v11.ShouldBeNull();
    [Fact] void should_admit_v12_enum_member_numbers() => _v12.ShouldBeNull();
    [Fact] void should_admit_v13_concept_compliance() => _v13.ShouldBeNull();
    [Fact] void should_refuse_v14_query_filters() => _newer!.Code.ShouldEqual("CLI-RENDER-004");
    [Fact] void should_name_the_refused_version() => _newer!.Message.ShouldContain("ESM v14.0");
    [Fact] void should_block_rendering_newer_versions() => _newer!.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Error);
    [Fact] void should_describe_the_explicit_version_gate() => _newer!.Message.ShouldContain("Newer model versions require explicit renderer admission");
}
