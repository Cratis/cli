// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.for_DatabaseSelection.given;
using Cratis.Cli.Templates;

namespace Cratis.Cli.for_DatabaseSelection.when_resolving;

public class and_the_request_differs_only_in_case : a_manifest
{
    DatabaseSelectionResult? _result;

    void Because() => _result = DatabaseSelection.Resolve(
        WithDatabaseChoices("MongoDB", "PostgreSQL", "MsSql", "SQLite"),
        requested: "POSTGRESQL",
        bound: new Dictionary<string, string>());

    [Fact] void should_match_the_choice_case_insensitively() =>
        _result!.Merge["Database"].ShouldEqual("PostgreSQL");

    [Fact] void should_have_no_errors() => _result!.Errors.ShouldBeEmpty();
}
