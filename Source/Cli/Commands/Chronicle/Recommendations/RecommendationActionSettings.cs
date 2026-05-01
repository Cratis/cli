// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Recommendations;

/// <summary>
/// Settings for recommendation action commands.
/// </summary>
public class RecommendationActionSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the recommendation ID.
    /// </summary>
    [CommandArgument(0, "<RECOMMENDATION_ID>")]
    [Description("Recommendation ID (GUID, from 'cratis recommendations list')")]
    public Guid RecommendationId { get; set; }
}
