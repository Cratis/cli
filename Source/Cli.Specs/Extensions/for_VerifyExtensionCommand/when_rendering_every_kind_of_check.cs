// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Extension;
using Cratis.Cli.Extensions.Verification;

namespace Cratis.Cli.Extensions.for_VerifyExtensionCommand;

public class when_rendering_every_kind_of_check : Specification
{
    string _failed = null!;
    string _correct = null!;

    static string Render(ExtensionVerifyReport report)
    {
        using var writer = new StringWriter();
        VerifyExtensionCommand.Render(report, writer);
        return writer.ToString().ReplaceLineEndings("\n");
    }

    void Because()
    {
        _failed = Render(new("demo", [
            new VerifyCheckResult("contract", VerifyStatus.Passed, []),
            new VerifyCheckResult("manifest", VerifyStatus.Passed, [new("manifest", "EXTV014", VerifySeverity.Warning, "no caption", "contract.play", 6)]),
            new VerifyCheckResult("contents", VerifyStatus.Failed, [
                new("contents", "EXTV022", VerifySeverity.Error, "'x' is shipped", "x"),
                new("contents", "EXTV023", VerifySeverity.Error, "nothing shipped")]),
            new VerifyCheckResult("frontend", VerifyStatus.Skipped, []),
            new VerifyCheckResult("backend", VerifyStatus.NotApplicable, [])]));
        _correct = Render(new("demo", [
            new VerifyCheckResult("manifest", VerifyStatus.Passed, [new("manifest", "EXTV014", VerifySeverity.Warning, "no caption", "contract.play")])]));
    }

    [Fact]
    void should_write_every_check_with_its_mark_note_and_diagnostics_then_the_verdict() => _failed.ShouldEqual(
        "Verifying demo\n" +
        "  ✓ contract\n" +
        "  ✓ manifest\n" +
        "      EXTV014 warning contract.play:6: no caption\n" +
        "  ✗ contents\n" +
        "      EXTV022 error x: 'x' is shipped\n" +
        "      EXTV023 error nothing shipped\n" +
        "  ○ frontend (could not run)\n" +
        "  - backend (not applicable)\n" +
        "Not correct: 2 error(s), 1 warning(s).\n");

    [Fact]
    void should_write_a_correct_verdict_with_the_warning_count() => _correct.ShouldEqual(
        "Verifying demo\n" +
        "  ✓ manifest\n" +
        "      EXTV014 warning contract.play: no caption\n" +
        "Correct (1 warning(s)).\n");
}
