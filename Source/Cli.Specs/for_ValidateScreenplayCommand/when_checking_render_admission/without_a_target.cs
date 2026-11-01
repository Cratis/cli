// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_checking_render_admission;

public class without_a_target : given.a_stage_refused_model
{
    async Task Because() => await Run("--executable");

    [Fact] void should_pass_binding_validation() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_not_report_stage_diagnostics() => _error.ShouldNotContain("STAGE-");
}
