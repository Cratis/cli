// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Verification;

namespace Cratis.Cli.Extensions.for_ExtensionVerifier;

public class when_the_package_has_no_code_to_run : given.a_valid_package_with_recording_runtime_checks
{
    async Task Because()
    {
        Write("contract.play", "extension Demo\n  display name \"Demo\"\n  description \"A demo\"\n  icon \"assets/icon.png\"\n  requires stage \"^5\"\n\n  ability Ping idempotent\n    text String\n");
        Report = await RunWith(VerifyStatus.Passed, VerifyStatus.Passed);
    }

    [Fact] void should_not_call_the_frontend_check() => Frontend.Calls.ShouldEqual(0);
    [Fact] void should_not_call_the_backend_check() => Backend.Calls.ShouldEqual(0);
}
