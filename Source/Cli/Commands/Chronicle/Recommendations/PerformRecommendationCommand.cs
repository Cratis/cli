// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Recommendations;

/// <summary>
/// Performs a recommendation.
/// </summary>
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
