// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Verification;

namespace Cratis.Cli.Extensions.for_ExtensionVerifier;

public class when_the_package_has_a_frontend_but_no_backend : given.a_valid_package_with_recording_runtime_checks
{
    async Task Because()
    {
        ChangeContract("  backend runtime dotnet 9 assembly \"backend/Demo.dll\"\n", string.Empty);
        Report = await RunWith(VerifyStatus.Passed, VerifyStatus.Passed);
    }

    [Fact] void should_call_the_frontend_check() => Frontend.Calls.ShouldEqual(1);
    [Fact] void should_not_call_the_backend_check() => Backend.Calls.ShouldEqual(0);
}
