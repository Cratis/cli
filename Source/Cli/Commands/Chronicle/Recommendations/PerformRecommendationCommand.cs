// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Recommendations;

/// <summary>
/// Performs a recommendation.
/// </summary>
[LlmDescription("Executes a Chronicle recommendation automatically. Use to apply a suggested maintenance action such as a schema migration or projection rebuild. Prompts for confirmation unless --yes is specified.")]
[CliCommand("perform", "Perform a recommendation", Branch = typeof(ChronicleBranch.Recommendations), DynamicCompletion = "recommendations")]
[CliExample("chronicle", "recommendations", "perform", "550e8400-e29b-41d4-a716-446655440000")]
[LlmOption("<RECOMMENDATION_ID>", "guid", "Recommendation identifier (positional)")]
public class PerformRecommendationCommand : ChronicleCommand<RecommendationActionSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, RecommendationActionSettings settings, string format)
    {
        if (!ConfirmationHelper.ShouldProceed(settings, $"Are you sure you want to perform recommendation '{settings.RecommendationId}'?"))
        {
            OutputFormatter.WriteMessage(format, "Aborted.");
            return ExitCodes.Success;
        }

        await services.Recommendations.Perform(new Perform
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            RecommendationId = settings.RecommendationId
        });

        OutputFormatter.WriteMessage(format, $"Recommendation '{settings.RecommendationId}' performed");
        return ExitCodes.Success;
    }
}
