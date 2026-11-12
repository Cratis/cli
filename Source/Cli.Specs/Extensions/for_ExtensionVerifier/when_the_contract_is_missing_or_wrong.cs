// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Verification;

namespace Cratis.Cli.Extensions.for_ExtensionVerifier;

public class when_the_contract_is_missing_or_wrong : given.a_valid_package
{
    ExtensionVerifyReport _missing = null!;
    ExtensionVerifyReport _wrong = null!;

    async Task Because()
    {
        var empty = Directory.CreateDirectory(Path.Combine(Folder, "empty")).FullName;
        _missing = await Verifier.Verify("empty", empty, new(), CancellationToken.None);
        ChangeContract("  ability Ping idempotent", "  gibberish here\n  ability Ping idempotent");
        _wrong = await Run();
    }

    [Fact] void should_fail_a_folder_without_a_contract() => _missing.Passed.ShouldBeFalse();
    [Fact] void should_say_the_contract_is_missing() => _missing.Diagnostics.Single().Code.ShouldEqual(ExtensionVerifyCodes.ContractMissing);
    [Fact] void should_still_list_every_check_for_a_folder_without_a_contract() => _missing.Checks.Count.ShouldEqual(ExtensionVerifyCodes.Checks.Count);
    [Fact] void should_fail_a_contract_that_does_not_compile() => _wrong.Passed.ShouldBeFalse();
    [Fact] void should_report_the_compiler_diagnostic_of_a_contract_that_does_not_compile() => _wrong.Checks.Single(check => check.Check == "contract").Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("PLAY0998");
    [Fact] void should_give_the_line_of_the_diagnostic() => _wrong.Checks.Single(check => check.Check == "contract").Diagnostics[0].Line.ShouldNotBeNull();
}
