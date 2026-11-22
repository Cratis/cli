// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildProgress;

public class when_validating_progress : Specification
{
    const string Credential = "fixture-value-known-only-to-the-host";
    string _step;
    string _line;

    void Because()
    {
        var body = new BuildProgress(Credential + new string('x', 2100), [new("Bearer fixture-authorization-value")])
            .ToJson("2", given.an_order.OrderId, new BuildRedaction([Credential]));
        _step = body["step"]!.GetValue<string>();
        _line = body["log"]![0]!["text"]!.GetValue<string>();
    }

    [Fact] void should_redact_known_credentials_before_clipping() => _step.Contains(Credential, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_limit_steps() => _step.Length.ShouldEqual(2000);
    [Fact] void should_redact_pattern_credentials() => _line.Contains("fixture-authorization-value", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_reject_non_uuid_identity() => Catch.Exception(() => new BuildProgress("Step").ToJson("2", "invalid", BuildRedaction.Patterns)).ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_reject_empty_reports() => Catch.Exception(() => new BuildProgress(" ").ToJson("2", given.an_order.OrderId, BuildRedaction.Patterns)).ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_reject_unknown_levels() => Catch.Exception(() => new BuildProgress(Log: [new("line", (BuildProgressLevel)23)]).ToJson("2", given.an_order.OrderId, BuildRedaction.Patterns)).ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_reject_overlarge_log_batches() => Catch.Exception(() => new BuildProgress(Log: [.. Enumerable.Repeat(new BuildProgressLine("line"), 101)]).ToJson("2", given.an_order.OrderId, BuildRedaction.Patterns)).ShouldBeOfExactType<ArgumentException>();
}
