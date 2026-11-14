// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Verification;

namespace Cratis.Cli.Extensions.for_ExtensionVerifier;

public class when_the_runtime_checks_fail : given.a_valid_package_with_recording_runtime_checks
{
    async Task Because() => Report = await RunWith(VerifyStatus.Failed, VerifyStatus.Failed);

    [Fact] void should_carry_the_frontend_result_unchanged() => Check("frontend").ShouldEqual(Frontend.Result);
    [Fact] void should_carry_the_backend_result_unchanged() => Check("backend").ShouldEqual(Backend.Result);
    [Fact] void should_report_the_frontend_error() => Codes("frontend").ShouldEqual([ExtensionVerifyCodes.FrontendNotRun]);
    [Fact] void should_report_the_backend_error() => Codes("backend").ShouldEqual([ExtensionVerifyCodes.BackendNotRun]);
    [Fact] void should_not_pass() => Report.Passed.ShouldBeFalse();
    [Fact] void should_not_pass_strictly() => Report.PassedStrictly.ShouldBeFalse();
}
