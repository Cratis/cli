// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Extension;
using Cratis.Cli.Extensions.Verification;

namespace Cratis.Cli.Extensions.for_VerifyExtensionCommand;

public class when_describing_a_report_as_json : Specification
{
    string _json = null!;
    string _strict = null!;

    void Because()
    {
        var report = new ExtensionVerifyReport("demo", [
            new VerifyCheckResult("manifest", VerifyStatus.Passed, [new("manifest", "EXTV014", VerifySeverity.Warning, "no caption", "contract.play", 6)]),
            new VerifyCheckResult("frontend", VerifyStatus.NotApplicable, [new("frontend", "EXTV030", VerifySeverity.Warning, "no frontend")])]);
        _json = JsonSerializer.Serialize(VerifyExtensionCommand.Describe(report, false), OutputFormatter.JsonSerializerOptions);
        _strict = JsonSerializer.Serialize(VerifyExtensionCommand.Describe(report, true), OutputFormatter.JsonSerializerOptions);
    }

    [Fact]
    void should_hold_the_target_verdict_and_every_check_with_its_diagnostics() => _json.ShouldEqual(
        "{\"target\":\"demo\",\"passed\":true,\"checks\":[" +
        "{\"check\":\"manifest\",\"status\":\"passed\",\"diagnostics\":[{\"check\":\"manifest\",\"code\":\"EXTV014\",\"severity\":\"warning\",\"message\":\"no caption\",\"path\":\"contract.play\",\"line\":6}]}," +
        "{\"check\":\"frontend\",\"status\":\"notapplicable\",\"diagnostics\":[{\"check\":\"frontend\",\"code\":\"EXTV030\",\"severity\":\"warning\",\"message\":\"no frontend\"}]}]}");

    [Fact] void should_fail_a_warned_report_when_strict() => _strict.ShouldContain("\"passed\":false");
}
