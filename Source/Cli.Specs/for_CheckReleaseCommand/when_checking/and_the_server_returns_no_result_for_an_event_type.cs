// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Cli.Commands.Chronicle.CheckRelease;

namespace Cratis.Cli.for_CheckReleaseCommand.when_checking;

public class and_the_server_returns_no_result_for_an_event_type : given.a_release_target
{
    async Task Because()
    {
        _schemaResult.Response = new CheckEventTypeRegistrationsResponse { Results = [] };
        await Check();
    }

    [Fact] void should_report_the_schemas_as_unknown() => Schemas.Verdict.ShouldEqual(CompatibilityVerdict.Unknown);
    [Fact] void should_name_the_missing_event_type() => Schemas.Actual.ShouldContain("BookAdded generation 1");
    [Fact] void should_not_be_compatible() => _result.Verdict.ShouldEqual(CompatibilityVerdict.Unknown);
}
