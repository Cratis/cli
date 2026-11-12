// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Verification;

namespace Cratis.Cli.Extensions.for_ExtensionVerifier;

public class when_verifying_a_correct_package : given.a_valid_package
{
    async Task Because() => Report = await Run();

    [Fact] void should_pass() => Report.Passed.ShouldBeTrue();
    [Fact] void should_report_every_check() => Report.Checks.Select(check => check.Check).ShouldEqual(ExtensionVerifyCodes.Checks);
    [Fact] void should_pass_the_contract() => Check("contract").Status.ShouldEqual(VerifyStatus.Passed);
    [Fact] void should_pass_the_manifest() => Check("manifest").Status.ShouldEqual(VerifyStatus.Passed);
    [Fact] void should_pass_the_contents() => Check("contents").Status.ShouldEqual(VerifyStatus.Passed);
    [Fact] void should_pass_the_placeholders() => Check("placeholders").Status.ShouldEqual(VerifyStatus.Passed);
    [Fact] void should_pass_the_compatibility() => Check("compatibility").Status.ShouldEqual(VerifyStatus.Passed);
    [Fact] void should_not_apply_the_digests_to_a_folder() => Check("digests").Status.ShouldEqual(VerifyStatus.NotApplicable);
    [Fact] void should_report_the_frontend_as_not_run_never_as_passed() => Check("frontend").Status.ShouldEqual(VerifyStatus.Skipped);
    [Fact] void should_report_the_backend_as_not_run_never_as_passed() => Check("backend").Status.ShouldEqual(VerifyStatus.Skipped);
    [Fact] void should_not_pass_strictly_while_a_check_could_not_run() => Report.PassedStrictly.ShouldBeFalse();
}
