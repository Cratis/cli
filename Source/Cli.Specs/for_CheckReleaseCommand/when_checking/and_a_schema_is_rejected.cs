// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Cli.Commands.Chronicle.CheckRelease;

namespace Cratis.Cli.for_CheckReleaseCommand.when_checking;

public class and_a_schema_is_rejected : given.a_release_target
{
    async Task Because()
    {
        _schemaResult.Response = new CheckEventTypeRegistrationsResponse
        {
            Results = [new EventTypeCheckResult { EventType = "BookAdded", Generation = 1, Accepted = false, Reason = "Property removed", Keyword = "required", Path = "/title" }]
        };
        await Check();
    }

    [Fact] void should_report_the_schemas_as_incompatible() => Schemas.Verdict.ShouldEqual(CompatibilityVerdict.Incompatible);
    [Fact] void should_name_the_path_keyword_and_reason() => Schemas.Actual.ShouldContain("/title (required): Property removed");
    [Fact] void should_not_advise_disabling_validation() => Schemas.Action.ShouldContain("do not disable schema validation");
    [Fact] void should_only_read() => _services.EventTypes.ReceivedCalls().All(_ => _.GetMethodInfo().Name == "CheckEventTypeRegistrations").ShouldBeTrue();
}
