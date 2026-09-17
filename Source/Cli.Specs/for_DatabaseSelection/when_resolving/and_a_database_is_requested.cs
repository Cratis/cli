// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.for_DatabaseSelection.given;
using Cratis.Cli.Templates;

namespace Cratis.Cli.for_DatabaseSelection.when_resolving;

public class and_a_database_is_requested : a_manifest
{
    DatabaseSelectionResult? _result;

    void Because() => _result = DatabaseSelection.Resolve(
        WithDatabaseChoices("MongoDB", "PostgreSQL", "MsSql", "SQLite"),
        requested: "postgresql",
        bound: new Dictionary<string, string>());

    [Fact] void should_merge_the_canonical_choice() =>
        _result!.Merge["Database"].ShouldEqual("PostgreSQL");

    [Fact] void should_have_no_errors() => _result!.Errors.ShouldBeEmpty();
}
