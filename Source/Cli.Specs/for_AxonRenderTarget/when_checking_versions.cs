// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AxonRenderTarget;

public class when_checking_versions : given.an_axon_target
{
    ScreenplayDiagnostic? _admitted;
    ScreenplayDiagnostic? _refused;
    ScreenplayDiagnostic? _cratis;

    void Because()
    {
        _admitted = _target.CheckSemanticVersion(new(34, 0));
        _refused = _target.CheckSemanticVersion(new(35, 0));
        _cratis = new CratisRenderTarget().CheckSemanticVersion(new(33, 0));
    }

    [Fact] void should_use_the_facades_admitted_ceiling() => _admitted.ShouldBeNull();
    [Fact] void should_refuse_a_newer_version() => _refused!.Code.ShouldEqual(RenderedSemanticVersions.NotAdmittedCode);
    [Fact] void should_identify_the_axon_target() => _refused!.Message.ShouldContain("--target axon");
    [Fact] void should_leave_cratis_admission_unchanged() => _cratis!.Code.ShouldEqual(RenderedSemanticVersions.NotAdmittedCode);
}
