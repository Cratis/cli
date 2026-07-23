// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueConfigurationBuilder.when_building;

public class with_api_source : Specification
{
    PrologueConfiguration _result;

    void Because() => _result = PrologueConfigurationBuilder.Build(new PrologueWizardInput(
        Guid.NewGuid(),
        [],
        [],
        new("/api", "http://my-system:8080/"),
        null,
        new(OutputKind.Json, "http://localhost:5005", "./captures")));

    [Fact] void should_have_a_reverse_proxy() => _result.ReverseProxy.ShouldNotBeNull();
    [Fact] void should_route_the_base_path_to_the_monitored_cluster() => _result.ReverseProxy!["Routes"]!["monitored"]!["Match"]!["Path"]!.GetValue<string>().ShouldEqual("/api/{**catch-all}");
    [Fact] void should_point_the_cluster_at_the_system() => _result.ReverseProxy!["Clusters"]!["monitored"]!["Destinations"]!["primary"]!["Address"]!.GetValue<string>().ShouldEqual("http://my-system:8080/");
}
