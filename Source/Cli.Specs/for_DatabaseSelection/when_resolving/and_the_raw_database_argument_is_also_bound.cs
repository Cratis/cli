// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.for_DatabaseSelection.given;
using Cratis.Cli.Templates;

namespace Cratis.Cli.for_DatabaseSelection.when_resolving;

public class and_the_raw_database_argument_is_also_bound : a_manifest
{
    DatabaseSelectionResult? _whenAgreeing;
    DatabaseSelectionResult? _whenConflicting;

    void Because()
    {
        var manifest = WithDatabaseChoices("MongoDB", "PostgreSQL");
        _whenAgreeing = DatabaseSelection.Resolve(manifest, requested: "postgresql", bound: new Dictionary<string, string> { ["Database"] = "PostgreSQL" });
        _whenConflicting = DatabaseSelection.Resolve(manifest, requested: "mssql", bound: new Dictionary<string, string> { ["Database"] = "MongoDB" });
    }

    [Fact] void should_resolve_agreeing_values_without_errors() => _whenAgreeing!.Errors.ShouldBeEmpty();

    [Fact] void should_error_on_conflicting_values_naming_both()
    {
        _whenConflicting!.Errors.Count.ShouldEqual(1);
        _whenConflicting.Errors[0].ShouldContain("conflicts with the --Database 'MongoDB'");
    }
}
