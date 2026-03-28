// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Recommendations;

/// <summary>
/// Ignores a recommendation.
/// </summary>
[CliCommand("ignore", "Ignore a recommendation", Branch = typeof(ChronicleBranch.Recommendations), DynamicCompletion = "recommendations")]
[CliExample("chronicle", "recommendations", "ignore", "550e8400-e29b-41d4-a716-446655440000")]
[LlmOption("<RECOMMENDATION_ID>", "guid", "Recommendation identifier (positional)")]
public class IgnoreRecommendationCommand : ChronicleCommand<RecommendationActionSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, RecommendationActionSettings settings, string format)
    {
        if (!ConfirmationHelper.ShouldProceed(settings, $"Are you sure you want to ignore recommendation '{settings.RecommendationId}'?"))
        {
            OutputFormatter.WriteMessage(format, "Aborted.");
            return ExitCodes.Success;
        }

        // The Ignore RPC reuses the Perform request message type per the protobuf contract.
        await services.Recommendations.Ignore(new Perform
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            RecommendationId = settings.RecommendationId
        });

        OutputFormatter.WriteMessage(format, $"Recommendation '{settings.RecommendationId}' ignored");
        return ExitCodes.Success;
    }
}
