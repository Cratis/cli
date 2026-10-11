// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_RenderedSemanticVersions;

public class when_checking_admitted_versions : Specification
{
    ScreenplayDiagnostic? _newer;
    ScreenplayDiagnostic? _admitted;

    void Because()
    {
        _newer = RenderedSemanticVersions.Check(SemanticVersion.V9);
        _admitted = RenderedSemanticVersions.Check(SemanticVersion.V8);
    }

    [Fact] void should_admit_v8() => _admitted.ShouldBeNull();
    [Fact] void should_refuse_versions_newer_than_v8() => _newer!.Code.ShouldEqual("CLI-RENDER-004");
    [Fact] void should_name_the_refused_version() => _newer!.Message.ShouldContain("ESM v9.0");
    [Fact] void should_block_rendering_newer_versions() => _newer!.Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Error);
    [Fact] void should_describe_the_explicit_version_gate() => _newer!.Message.ShouldContain("Newer model versions require explicit renderer admission");
}
