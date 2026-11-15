// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Verification;

namespace Cratis.Cli.Extensions.for_ExtensionVerifier;

public class when_the_runtime_checks_are_skipped : given.a_valid_package_with_recording_runtime_checks
{
    async Task Because() => Report = await RunWith(VerifyStatus.Skipped, VerifyStatus.Skipped);

    [Fact] void should_carry_the_frontend_result_unchanged() => Check("frontend").ShouldEqual(Frontend.Result);
    [Fact] void should_carry_the_backend_result_unchanged() => Check("backend").ShouldEqual(Backend.Result);
    [Fact] void should_pass() => Report.Passed.ShouldBeTrue();
    [Fact] void should_not_pass_strictly() => Report.PassedStrictly.ShouldBeFalse();
}
