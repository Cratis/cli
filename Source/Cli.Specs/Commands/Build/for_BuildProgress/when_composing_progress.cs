// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildProgress;

public class when_composing_progress : Specification
{
    JsonObject _body;

    void Because() => _body = new BuildProgress("Building", [new("A line", BuildProgressLevel.Warning)], new(12, 4))
        .ToJson("2", given.an_order.OrderId, BuildRedaction.Patterns);

    [Fact] void should_stamp_the_order() => _body["orderId"]!.GetValue<string>().ShouldEqual(given.an_order.OrderId);
    [Fact] void should_use_the_build_contract_version() => _body["contractVersion"]!.GetValue<string>().ShouldEqual("2");
    [Fact] void should_use_string_log_levels() => _body["log"]![0]!["level"]!.GetValue<string>().ShouldEqual("warning");
    [Fact] void should_include_cumulative_usage() => _body["usage"]!["inputTokens"]!.GetValue<long>().ShouldEqual(12L);
    [Fact] void should_include_an_occurrence_time() => DateTimeOffset.TryParse(_body["log"]![0]!["occurred"]!.GetValue<string>(), out _).ShouldBeTrue();
}
