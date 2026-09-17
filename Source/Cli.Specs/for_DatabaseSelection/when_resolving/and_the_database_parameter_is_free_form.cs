// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.for_DatabaseSelection.given;
using Cratis.Cli.Templates;

namespace Cratis.Cli.for_DatabaseSelection.when_resolving;

public class and_the_database_parameter_is_free_form : a_manifest
{
    DatabaseSelectionResult? _result;

    void Because() => _result = DatabaseSelection.Resolve(
        WithFreeFormDatabase(),
        requested: "SQLite",
        bound: new Dictionary<string, string>());

    [Fact] void should_merge_the_normalized_lower_case_form() =>
        _result!.Merge["Database"].ShouldEqual("sqlite");

    [Fact] void should_have_no_errors() => _result!.Errors.ShouldBeEmpty();
}
