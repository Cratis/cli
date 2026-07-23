// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueConfigurationBuilder.when_building;

public class with_sql_server_sources : Specification
{
    static readonly Guid _prologueId = Guid.NewGuid();
    PrologueConfiguration _result;

    void Because() => _result = PrologueConfigurationBuilder.Build(new PrologueWizardInput(
        _prologueId,
        [
            new("main", "Server=main;Database=Library;", ["Authors", "Books"]),
            new("audit", "Server=audit;Database=Audit;", [])
        ],
        [],
        null,
        null,
        new(OutputKind.Json, "http://localhost:5005", "./captures")));

    [Fact] void should_carry_the_prologue_id() => _result.Prologue.PrologueId.ShouldEqual(_prologueId);
    [Fact] void should_have_both_sources() => _result.Prologue.SqlServer.Count.ShouldEqual(2);
    [Fact] void should_carry_the_first_source_name() => _result.Prologue.SqlServer[0].Name.ShouldEqual("main");
    [Fact] void should_carry_the_first_source_connection_string() => _result.Prologue.SqlServer[0].ConnectionString.ShouldEqual("Server=main;Database=Library;");
    [Fact] void should_carry_the_first_source_tables() => _result.Prologue.SqlServer[0].Tables.ShouldEqual("Authors", "Books");
    [Fact] void should_leave_the_second_source_tables_empty_for_all_tables() => _result.Prologue.SqlServer[1].Tables.ShouldBeEmpty();
    [Fact] void should_not_have_postgres_sources() => _result.Prologue.Postgres.ShouldBeEmpty();
    [Fact] void should_not_have_a_reverse_proxy() => _result.ReverseProxy.ShouldBeNull();
}
