// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Verification;

namespace Cratis.Cli.Extensions.for_ExtensionVerifier;

public class when_the_package_has_no_backend_or_frontend : given.a_valid_package
{
    async Task Because()
    {
        Write("contract.play", "extension Demo\n  display name \"Demo\"\n  description \"A demo\"\n  icon \"assets/icon.png\"\n  requires stage \"^5\"\n\n  ability Ping idempotent\n    text String\n");
        Report = await Run();
    }

    [Fact] void should_not_apply_the_frontend_check() => Check("frontend").Status.ShouldEqual(VerifyStatus.NotApplicable);
    [Fact] void should_not_apply_the_backend_check() => Check("backend").Status.ShouldEqual(VerifyStatus.NotApplicable);
    [Fact] void should_flag_the_files_the_contract_does_not_declare() => Codes("contents").ShouldContain("EXTV022");
}
