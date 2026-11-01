// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueConfigurationBuilder.when_building;

public class without_named_services : Specification
{
    PrologueConfiguration _result;

    void Because() => _result = PrologueConfigurationBuilder.Build(new PrologueWizardInput(
        Guid.NewGuid(),
        [new("main", "Server=main", [])],
        [],
        new("/api", "http://my-system:8080/"),
        null,
        new(OutputKind.Json, "http://localhost:5005", "./captures")));

    [Fact] void should_name_no_services() => _result.Prologue.Services.ShouldBeEmpty();
    [Fact] void should_keep_the_databases_on_the_default_service() => _result.Prologue.SqlServer[0].Name.ShouldEqual("main");
    [Fact] void should_keep_only_the_default_route() => _result.ReverseProxy["Routes"].AsObject().Select(route => route.Key).ShouldContainOnly([PrologueConfigurationBuilder.DefaultCluster]);
}
