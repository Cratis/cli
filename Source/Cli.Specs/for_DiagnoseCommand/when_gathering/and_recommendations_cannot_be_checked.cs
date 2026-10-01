// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Recommendations;

namespace Cratis.Cli.for_DiagnoseCommand.when_gathering;

[Collection(CliSpecsCollection.Name)]
public class and_recommendations_cannot_be_checked : given.healthy_services
{
    void Establish() => _services.Recommendations.GetRecommendations(Arg.Any<GetRecommendationsRequest>()).Returns(
        QueryResult<IEnumerable<RecommendationDetailsResponse>>.Error(Guid.Empty, new Exception("Recommendation query failed")));

    async Task Because() => _data = await DiagnoseCommand.Gather(_services, _settings);

    [Fact] void should_be_unhealthy() => _data.IsHealthy.ShouldBeFalse();
    [Fact] void should_name_the_check() => _data.ChecksCouldNotRun.Single().Check.ShouldEqual("Recommendations");
    [Fact] void should_preserve_the_reason() => _data.ChecksCouldNotRun.Single().Reason.ShouldContain("Recommendation query failed");
    [Fact] void should_exit_with_server_error() => _data.ExitCode.ShouldEqual(ExitCodes.ServerError);
}
