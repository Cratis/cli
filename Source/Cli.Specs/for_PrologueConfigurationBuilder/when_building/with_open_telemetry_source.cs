// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueConfigurationBuilder.when_building;

public class with_open_telemetry_source : Specification
{
    PrologueConfiguration _result;

    void Because() => _result = PrologueConfigurationBuilder.Build(new PrologueWizardInput(
        Guid.NewGuid(),
        [],
        [],
        null,
        new(["core", "billing"], ["http.route"], "http://collector:4318", "http://collector:4317"),
        new(OutputKind.Json, "http://localhost:5005", "./captures")));

    [Fact] void should_enable_open_telemetry() => _result.Prologue.OpenTelemetry.Enabled.ShouldBeTrue();
    [Fact] void should_carry_the_service_names() => _result.Prologue.OpenTelemetry.ServiceNames.ShouldEqual("core", "billing");
    [Fact] void should_carry_the_attribute_keys() => _result.Prologue.OpenTelemetry.AttributeKeys.ShouldEqual("http.route");
    [Fact] void should_carry_the_upstream_http_collector() => _result.Prologue.OpenTelemetry.Upstream.Http.ShouldEqual("http://collector:4318");
    [Fact] void should_carry_the_upstream_grpc_collector() => _result.Prologue.OpenTelemetry.Upstream.Grpc.ShouldEqual("http://collector:4317");
}
