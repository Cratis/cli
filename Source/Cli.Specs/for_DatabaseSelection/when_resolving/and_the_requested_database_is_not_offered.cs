// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.for_DatabaseSelection.given;
using Cratis.Cli.Templates;

namespace Cratis.Cli.for_DatabaseSelection.when_resolving;

public class and_the_requested_database_is_not_offered : a_manifest
{
    DatabaseSelectionResult? _result;

    void Because() => _result = DatabaseSelection.Resolve(
        WithDatabaseChoices("MongoDB", "PostgreSQL"),
        requested: "mssql",
        bound: new Dictionary<string, string>());

    [Fact] void should_error_listing_the_offered_databases() =>
        _result!.Errors[0].ShouldContain("Offered databases: MongoDB, PostgreSQL.");

    [Fact] void should_merge_nothing() => _result!.Merge.ShouldBeEmpty();
}
