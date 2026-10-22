// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_checking_render_admission;

public class with_render_admission_disabled : given.a_stage_refused_model
{
    async Task Because() => await Run("--executable", "--target", "cratis", "--no-render-admission");

    [Fact] void should_pass_binding_validation() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_not_run_admission()
    {
        using var document = JsonDocument.Parse(_output);
        var admission = document.RootElement.GetProperty("targetAdmission");
        admission.GetProperty("status").GetString().ShouldEqual("skipped");
        admission.GetProperty("diagnostics").GetArrayLength().ShouldEqual(0);
        admission.GetProperty("refusals").ValueKind.ShouldEqual(JsonValueKind.Null);
        admission.GetProperty("implementationGaps").ValueKind.ShouldEqual(JsonValueKind.Null);
    }
    [Fact] void should_not_report_stage_diagnostics() => _error.ShouldNotContain("STAGE-");
}
