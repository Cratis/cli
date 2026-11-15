// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// What Studio answered when a builder registered.
/// </summary>
/// <param name="BuilderId">The builder's identifier.</param>
/// <param name="Code">The short code a maintainer types to approve the builder.</param>
/// <param name="Expires">When the registration expires unless approved.</param>
public sealed record StudioBuilderRegistration(string BuilderId, string Code, DateTimeOffset Expires);

/// <summary>
/// What Studio agreed when a builder connected.
/// </summary>
/// <param name="Name">What maintainers see the builder as.</param>
/// <param name="ProtocolVersion">The protocol version agreed.</param>
/// <param name="NeedsUpgrade">Whether Studio speaks a newer version the builder should be upgraded to.</param>
/// <param name="WaitSeconds">How long Studio holds a request for work open.</param>
/// <param name="LeaseSeconds">How long a claim holds without a heartbeat.</param>
public sealed record StudioBuilderWelcome(string Name, int ProtocolVersion, bool NeedsUpgrade, int WaitSeconds, int LeaseSeconds);

/// <summary>
/// A slice Studio gave the builder to build. The claim is already made.
/// </summary>
/// <param name="SliceId">The slice.</param>
/// <param name="LeaseExpires">When the claim runs out without a heartbeat.</param>
/// <param name="Git">Who does the repository work, and how, for this build.</param>
/// <param name="Order">The build order.</param>
public sealed record StudioBuilderWork(string SliceId, DateTimeOffset LeaseExpires, BuildGitInstructions Git, BuildOrder Order);

/// <summary>
/// The exception that is thrown when Studio refuses a builder's request.
/// </summary>
/// <param name="status">The HTTP status Studio answered with.</param>
/// <param name="reason">Studio's reason.</param>
/// <param name="problems">The problems Studio named, if any.</param>
/// <param name="code">The registration code, when the registration waits for approval.</param>
public sealed class StudioBuilderRefused(HttpStatusCode status, string reason, IReadOnlyList<string> problems, string? code = null)
    : Exception(problems.Count == 0 ? reason : $"{reason} {string.Join(' ', problems)}")
{
    /// <summary>Gets the HTTP status Studio answered with.</summary>
    public HttpStatusCode Status { get; } = status;

    /// <summary>Gets Studio's reason.</summary>
    public string Reason { get; } = reason;

    /// <summary>Gets the problems Studio named.</summary>
    public IReadOnlyList<string> Problems { get; } = problems;

    /// <summary>Gets the registration code, when the registration waits for approval.</summary>
    public string? Code { get; } = code;

    /// <summary>Gets a value indicating whether the registration waits for a maintainer to approve it.</summary>
    public bool AwaitsApproval => Status == HttpStatusCode.Accepted;

    /// <summary>Gets a value indicating whether the builder's credential is no longer accepted.</summary>
    public bool CredentialRejected => Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;

    /// <summary>
    /// Reads a refusal from Studio's answer.
    /// </summary>
    /// <param name="status">The status.</param>
    /// <param name="body">The body, which may be empty.</param>
    /// <returns>The refusal.</returns>
    public static StudioBuilderRefused From(HttpStatusCode status, string body)
    {
        JsonObject? answer = null;
        try
        {
            answer = string.IsNullOrWhiteSpace(body) ? null : JsonNode.Parse(body) as JsonObject;
        }
        catch (JsonException)
        {
        }

        var reason = answer?["reason"]?.GetValue<string>() ?? $"Studio answered {(int)status} {status}.";
        var problems = (answer?["problems"] as JsonArray ?? []).Select(problem => problem?.ToString()).OfType<string>().ToList();
        return new(status, reason, problems, answer?["code"]?.GetValue<string>());
    }
}
