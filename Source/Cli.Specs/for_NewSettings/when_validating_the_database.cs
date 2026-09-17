// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.New;
using Spectre.Console.Cli;

namespace Cratis.Cli.for_NewSettings;

public class when_validating_the_database : Specification
{
    [Fact] void should_accept_all_supported_backends() =>
        new[] { "mongodb", "PostgreSQL", "MSSQL", "sqliTe", "MongoDB" }
            .All(database => new NewSettings { Database = database }.Validate().Successful)
            .ShouldBeTrue();

    [Fact] void should_reject_an_unsupported_backend() =>
        new NewSettings { Database = "mysql" }.Validate().Successful.ShouldBeFalse();

    [Fact] void should_accept_an_absent_database() =>
        new NewSettings().Validate().Successful.ShouldBeTrue();
}
