// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Verification;

namespace Cratis.Cli.Extensions.for_ExtensionVerifier;

public class when_the_runtime_checks_pass : given.a_valid_package_with_recording_runtime_checks
{
    async Task Because() => Report = await RunWith(VerifyStatus.Passed, VerifyStatus.Passed);

    [Fact] void should_run_the_frontend_check_once() => Frontend.Calls.ShouldEqual(1);
    [Fact] void should_run_the_backend_check_once() => Backend.Calls.ShouldEqual(1);
    [Fact] void should_carry_the_frontend_result_unchanged() => Check("frontend").ShouldEqual(Frontend.Result);
    [Fact] void should_carry_the_backend_result_unchanged() => Check("backend").ShouldEqual(Backend.Result);
    [Fact] void should_pass() => Report.Passed.ShouldBeTrue();
    [Fact] void should_pass_strictly() => Report.PassedStrictly.ShouldBeTrue();
}
