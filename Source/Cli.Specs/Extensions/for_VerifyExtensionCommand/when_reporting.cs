// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Extension;
using Cratis.Cli.Extensions.Verification;

namespace Cratis.Cli.Extensions.for_VerifyExtensionCommand;

public class when_reporting : Specification
{
    ExtensionVerifyReport _correct = null!;
    ExtensionVerifyReport _warned = null!;
    ExtensionVerifyReport _failed = null!;
    string _text = null!;

    static ExtensionVerifyReport Report(params VerifyCheckResult[] checks) => new("demo", checks);

    void Because()
    {
        _correct = Report(new VerifyCheckResult("contract", VerifyStatus.Passed, []));
        _warned = Report(new VerifyCheckResult("manifest", VerifyStatus.Passed, [new("manifest", "EXTV014", VerifySeverity.Warning, "no caption", "contract.play", 6)]));
        _failed = Report(new VerifyCheckResult("contents", VerifyStatus.Failed, [new("contents", "EXTV022", VerifySeverity.Error, "'x' is shipped", "x")]), new VerifyCheckResult("frontend", VerifyStatus.Skipped, []), new VerifyCheckResult("backend", VerifyStatus.NotApplicable, []));
        using var writer = new StringWriter();
        VerifyExtensionCommand.Render(_failed, writer);
        _text = writer.ToString();
    }

    [Fact] void should_succeed_for_a_correct_package() => VerifyExtensionCommand.ExitCodeOf(_correct, false).ShouldEqual(ExitCodes.Success);
    [Fact] void should_succeed_for_a_warning() => VerifyExtensionCommand.ExitCodeOf(_warned, false).ShouldEqual(ExitCodes.Success);
    [Fact] void should_fail_for_a_warning_when_strict() => VerifyExtensionCommand.ExitCodeOf(_warned, true).ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_fail_for_an_error() => VerifyExtensionCommand.ExitCodeOf(_failed, false).ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_fail_for_a_check_that_could_not_run_when_strict() => VerifyExtensionCommand.ExitCodeOf(Report(new VerifyCheckResult("frontend", VerifyStatus.Skipped, [])), true).ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_write_the_diagnostic_with_its_code_and_path() => _text.ShouldContain("EXTV022 error x: 'x' is shipped");
    [Fact] void should_write_a_check_that_could_not_run_as_such() => _text.ShouldContain("○ frontend (could not run)");
    [Fact] void should_write_the_verdict() => _text.ShouldContain("Not correct: 1 error(s), 0 warning(s).");
}
