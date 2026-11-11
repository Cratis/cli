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
    ScreenplayDiagnostic? _v14;
    ScreenplayDiagnostic? _v15;
    ScreenplayDiagnostic? _v16;
    ScreenplayDiagnostic? _v20;
    ScreenplayDiagnostic? _v21;
    ScreenplayDiagnostic? _v22;
    ScreenplayDiagnostic? _v23;
    ScreenplayDiagnostic? _v24;
    ScreenplayDiagnostic? _v25;
    ScreenplayDiagnostic? _v26;
    ScreenplayDiagnostic? _v27;
    ScreenplayDiagnostic? _v28;
    ScreenplayDiagnostic? _v29;
    ScreenplayDiagnostic? _v30;
    ScreenplayDiagnostic? _v31;
    ScreenplayDiagnostic? _v32;
    ScreenplayDiagnostic? _v33;
    ScreenplayDiagnostic? _v34;
    ScreenplayDiagnostic? _v36;
    ScreenplayDiagnostic? _v38;
    ScreenplayDiagnostic? _v49;
    ScreenplayDiagnostic? _v50;

    void Because()
    {
        _newer = RenderedSemanticVersions.Check(new SemanticVersion(35, 0));
        _v10 = RenderedSemanticVersions.Check(SemanticVersion.V10);
        _v11 = RenderedSemanticVersions.Check(SemanticVersion.V11);
        _v12 = RenderedSemanticVersions.Check(SemanticVersion.V12);
        _v13 = RenderedSemanticVersions.Check(SemanticVersion.V13);
        _v14 = RenderedSemanticVersions.Check(SemanticVersion.V14);
        _v15 = RenderedSemanticVersions.Check(SemanticVersion.V15);
        _v16 = RenderedSemanticVersions.Check(SemanticVersion.V16);
        _v20 = RenderedSemanticVersions.Check(SemanticVersion.V20);
        _v21 = RenderedSemanticVersions.Check(SemanticVersion.V21);
        _v22 = RenderedSemanticVersions.Check(SemanticVersion.V22);
        _v23 = RenderedSemanticVersions.Check(SemanticVersion.V23);
        _v24 = RenderedSemanticVersions.Check(SemanticVersion.V24);
        _v25 = RenderedSemanticVersions.Check(SemanticVersion.V25);
        _v26 = RenderedSemanticVersions.Check(SemanticVersion.V26);
        _v27 = RenderedSemanticVersions.Check(SemanticVersion.V27);
        _v28 = RenderedSemanticVersions.Check(SemanticVersion.V28);
        _v29 = RenderedSemanticVersions.Check(SemanticVersion.V29);
        _v30 = RenderedSemanticVersions.Check(SemanticVersion.V30);
        _v31 = RenderedSemanticVersions.Check(SemanticVersion.V31);
        _v32 = RenderedSemanticVersions.Check(SemanticVersion.V32);
        _v33 = RenderedSemanticVersions.Check(SemanticVersion.V33);
        _v34 = RenderedSemanticVersions.Check(SemanticVersion.V34);
        _v36 = RenderedSemanticVersions.Check(SemanticVersion.V36);
        _v38 = RenderedSemanticVersions.Check(SemanticVersion.V38);
        _v49 = RenderedSemanticVersions.Check(SemanticVersion.V49);
        _v50 = RenderedSemanticVersions.Check(new SemanticVersion(50, 0));
    }

    [Fact] void should_keep_v10_admitted() => _v10.ShouldBeNull();
    [Fact] void should_admit_v11_exact_numbers() => _v11.ShouldBeNull();
    [Fact] void should_admit_v12_enum_member_numbers() => _v12.ShouldBeNull();
    [Fact] void should_admit_v13_concept_compliance() => _v13.ShouldBeNull();
    [Fact] void should_admit_v14_query_filters() => _v14.ShouldBeNull();
    [Fact] void should_admit_v15_concept_generation_strategies() => _v15.ShouldBeNull();
    [Fact] void should_admit_v16_stored_event_names() => _v16.ShouldBeNull();
    [Fact] void should_admit_v20() => _v20.ShouldBeNull();
    [Fact] void should_admit_v21() => _v21.ShouldBeNull();
    [Fact] void should_admit_v22() => _v22.ShouldBeNull();
    [Fact] void should_admit_v23() => _v23.ShouldBeNull();
    [Fact] void should_admit_v24() => _v24.ShouldBeNull();
    [Fact] void should_admit_v25() => _v25.ShouldBeNull();
    [Fact] void should_admit_v26() => _v26.ShouldBeNull();
    [Fact] void should_admit_v27() => _v27.ShouldBeNull();
    [Fact] void should_admit_v28_uuid_comparisons() => _v28.ShouldBeNull();
    [Fact] void should_admit_v29() => _v29.ShouldBeNull();
    [Fact] void should_admit_v30() => _v30.ShouldBeNull();
    [Fact] void should_hand_v31_to_the_renderer() => _v31.ShouldBeNull();
    [Fact] void should_admit_v32_typed_capture_keys() => _v32.ShouldBeNull();
    [Fact] void should_admit_audited_v33() => _v33.ShouldBeNull();
    [Fact] void should_admit_audited_v34() => _v34.ShouldBeNull();
    [Fact] void should_admit_audited_v36() => _v36.ShouldBeNull();
    [Fact] void should_admit_audited_v38() => _v38.ShouldBeNull();
    [Fact] void should_admit_audited_v49() => _v49.ShouldBeNull();
    [Fact] void should_refuse_versions_newer_than_v49() => _v50.Code.ShouldEqual("CLI-RENDER-004");
    [Fact] void should_refuse_the_unaudited_v35() => _newer.Code.ShouldEqual("CLI-RENDER-004");
    [Fact] void should_name_the_refused_version() => _newer.Message.ShouldContain("ESM v35.0");
    [Fact] void should_block_rendering_newer_versions() => _newer.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Error);
    [Fact] void should_describe_the_explicit_version_gate() => _newer.Message.ShouldContain("Model versions outside that set require explicit renderer admission");
}
