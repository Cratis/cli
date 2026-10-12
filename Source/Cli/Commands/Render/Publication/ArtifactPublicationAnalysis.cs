// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Cli.Commands.Render.Publication;

internal sealed record ArtifactPublicationRefusal(string? Path, string Reason, string? Code = null);

internal sealed record ArtifactPublicationVerdict(string? Path, string Kind, string? BeforeSha256, string? AfterSha256, string? Reason = null)
{
    public IReadOnlyList<ArtifactSource> Sources { get; init; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Code { get; init; }
}

internal sealed record ArtifactPublicationAnalysis(
    PreparedArtifactPublication? Prepared,
    IReadOnlyList<ArtifactPublicationVerdict> Verdicts,
    IReadOnlyList<ArtifactPublicationRefusal> Refusals);
