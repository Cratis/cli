// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Recommendations;

/// <summary>
/// Ignores a recommendation.
/// </summary>
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
