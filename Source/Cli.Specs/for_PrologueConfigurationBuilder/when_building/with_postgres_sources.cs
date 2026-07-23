// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueConfigurationBuilder.when_building;

public class with_postgres_sources : Specification
{
    PrologueConfiguration _result;

    void Because() => _result = PrologueConfigurationBuilder.Build(new PrologueWizardInput(
        Guid.NewGuid(),
        [],
        [new("postgres", "Host=localhost;Database=library;")],
        null,
        null,
        new(OutputKind.Json, "http://localhost:5005", "./captures")));

    [Fact] void should_have_the_source() => _result.Prologue.Postgres.Count.ShouldEqual(1);
    [Fact] void should_carry_the_name() => _result.Prologue.Postgres[0].Name.ShouldEqual("postgres");
    [Fact] void should_carry_the_connection_string() => _result.Prologue.Postgres[0].ConnectionString.ShouldEqual("Host=localhost;Database=library;");
    [Fact] void should_keep_the_default_slot() => _result.Prologue.Postgres[0].Slot.ShouldEqual(new PostgresOptions().Slot);
    [Fact] void should_keep_the_default_publication() => _result.Prologue.Postgres[0].Publication.ShouldEqual(new PostgresOptions().Publication);
}
