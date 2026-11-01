// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueConfigurationBuilder.when_building;

public class with_named_services : Specification
{
    PrologueConfiguration _result;

    void Because() => _result = PrologueConfigurationBuilder.Build(new PrologueWizardInput(
        Guid.NewGuid(),
        [],
        [],
        null,
        new([], ["http.route"], "", ""),
        new(OutputKind.Json, "http://localhost:5005", "./captures"),
        [
            new("orders", [new("orders", "Server=orders", [])], [], new("/orders", "http://orders:8080/"), []),
            new("billing", [], [new("billing", "Host=billing")], new("/billing", "http://billing:8080/"), ["Billing.Api"]),
        ]));

    [Fact] void should_name_every_service() => _result.Prologue.Services.Keys.ShouldContainOnly(["orders", "billing"]);
    [Fact] void should_keep_the_default_service_without_databases() => _result.Prologue.SqlServer.ShouldBeEmpty();
    [Fact] void should_give_a_service_its_own_sql_server_databases() => _result.Prologue.Services["orders"].SqlServer[0].ConnectionString.ShouldEqual("Server=orders");
    [Fact] void should_give_a_service_its_own_postgres_databases() => _result.Prologue.Services["billing"].Postgres[0].ConnectionString.ShouldEqual("Host=billing");
    [Fact] void should_carry_the_telemetry_service_names() => _result.Prologue.Services["billing"].TelemetryServiceNames.ShouldContainOnly(["Billing.Api"]);
    [Fact] void should_route_each_service_to_a_cluster_named_after_it() => _result.ReverseProxy["Routes"]["billing"]["ClusterId"].GetValue<string>().ShouldEqual("billing");
    [Fact] void should_route_each_services_base_path() => _result.ReverseProxy["Routes"]["orders"]["Match"]["Path"].GetValue<string>().ShouldEqual("/orders/{**catch-all}");
    [Fact] void should_point_each_cluster_at_its_service() => _result.ReverseProxy["Clusters"]["orders"]["Destinations"]["primary"]["Address"].GetValue<string>().ShouldEqual("http://orders:8080/");
    [Fact] void should_not_add_the_default_services_route() => _result.ReverseProxy["Routes"].AsObject().ContainsKey(PrologueConfigurationBuilder.DefaultCluster).ShouldBeFalse();
    [Fact] void should_write_the_service_names_as_entered() => PrologueConfigurationFile.Write(_result).ShouldContain("\"orders\": {");
}
