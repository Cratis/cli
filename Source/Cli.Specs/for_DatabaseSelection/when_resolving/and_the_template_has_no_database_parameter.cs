// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.for_DatabaseSelection.given;
using Cratis.Cli.Templates;

namespace Cratis.Cli.for_DatabaseSelection.when_resolving;

public class and_the_template_has_no_database_parameter : a_manifest
{
    DatabaseSelectionResult? _whenRequested;
    DatabaseSelectionResult? _whenAbsent;

    void Because()
    {
        _whenRequested = DatabaseSelection.Resolve(WithoutDatabaseParameter(), requested: "postgresql", bound: new Dictionary<string, string>());
        _whenAbsent = DatabaseSelection.Resolve(WithoutDatabaseParameter(), requested: null, bound: new Dictionary<string, string>());
    }

    [Fact] void should_error_on_an_explicit_request_naming_the_template() =>
        _whenRequested!.Errors[0].ShouldContain("template 'plain' does not support --database");

    [Fact] void should_pass_an_absent_request_through_untouched()
    {
        _whenAbsent!.Merge.ShouldBeEmpty();
        _whenAbsent.Errors.ShouldBeEmpty();
    }
}
