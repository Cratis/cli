// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// Speaks Studio's builder connection contract: register, connect, ask for work, renew a lease, report an outcome and
/// disconnect. Every request goes from the builder to Studio, at one address per organization.
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="studio">The Studio address.</param>
/// <param name="organization">The organization's name in Studio.</param>
public sealed class StudioBuilderClient(HttpClient http, Uri studio, string organization)
{
    /// <summary>The protocol versions this builder speaks.</summary>
    public static readonly IReadOnlyList<int> ProtocolVersions = [1];

    string? _credential;

    /// <summary>
    /// Gets the address every request is relative to.
    /// </summary>
    public Uri Address { get; } = new($"{studio.ToString().TrimEnd('/')}/builders/connect/{Uri.EscapeDataString(organization)}/");

    /// <summary>
    /// Uses a builder's credential for every later request.
    /// </summary>
    /// <param name="builderId">The builder's identifier.</param>
    /// <param name="token">The token the builder created.</param>
    public void Authenticate(string builderId, string token) => _credential = $"{builderId}.{token}";

    /// <summary>
    /// Registers a builder with the organization.
    /// </summary>
    /// <param name="name">What maintainers see the builder as.</param>
    /// <param name="machine">The machine it runs on.</param>
    /// <param name="token">The token the builder created.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The builder's identifier and the code to approve it with.</returns>
    /// <exception cref="StudioBuilderRefused">Studio refused the registration.</exception>
    public async Task<StudioBuilderRegistration> Register(string name, string machine, string token, CancellationToken cancellationToken)
    {
        var (status, body) = await Send("registrations", new JsonObject { ["name"] = name, ["machine"] = machine, ["token"] = token }, false, cancellationToken);
        if (status != HttpStatusCode.Created && status != HttpStatusCode.OK) throw StudioBuilderRefused.From(status, body);
        var answer = Parse(body);
        return new(Text(answer["builderId"]), Text(answer["code"]), DateTimeOffset.Parse(Text(answer["expires"]), System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Connects the builder and agrees a protocol version.
    /// </summary>
    /// <param name="manifest">What the builder can build.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>What Studio agreed.</returns>
    /// <exception cref="StudioBuilderRefused">Studio refused; <see cref="StudioBuilderRefused.AwaitsApproval"/> while a maintainer has not yet approved the builder.</exception>
    public async Task<StudioBuilderWelcome> Connect(JsonObject manifest, CancellationToken cancellationToken)
    {
        var hello = new JsonObject { ["protocolVersions"] = new JsonArray([.. ProtocolVersions.Select(version => (JsonNode)version)]), ["manifest"] = manifest.DeepClone() };
        var (status, body) = await Send("connect", hello, true, cancellationToken);
        if (status != HttpStatusCode.OK) throw StudioBuilderRefused.From(status, body);
        var answer = Parse(body);
        return new(
            Text(answer["name"]),
            Number(answer["protocolVersion"], 1),
            answer["needsUpgrade"] is JsonValue upgrade && upgrade.TryGetValue<bool>(out var needsUpgrade) && needsUpgrade,
            Number(answer["waitSeconds"], 20),
            Number(answer["leaseSeconds"], 120));
    }

    /// <summary>
    /// Asks for a slice to build. Studio holds the request open until there is one, or answers that there is none yet.
    /// </summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The claimed slice, or <see langword="null"/> when there is nothing yet.</returns>
    /// <exception cref="StudioBuilderRefused">Studio refused the request.</exception>
    public async Task<StudioBuilderWork?> Work(CancellationToken cancellationToken)
    {
        var (status, body) = await Send("work", null, true, cancellationToken);
        if (status == HttpStatusCode.NoContent) return null;
        if (status != HttpStatusCode.OK) throw StudioBuilderRefused.From(status, body);
        var answer = Parse(body);
        return new(
            Text(answer["sliceId"]),
            DateTimeOffset.Parse(Text(answer["leaseExpires"]), System.Globalization.CultureInfo.InvariantCulture),
            BuildGitInstructions.From(answer["git"]),
            BuildOrder.From(answer["order"]?.DeepClone()));
    }

    /// <summary>
    /// Renews the lease on a slice being built.
    /// </summary>
    /// <param name="sliceId">The slice.</param>
    /// <param name="orderId">The order being built.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns><see langword="true"/> while the lease holds; <see langword="false"/> once it is lost and the build must stop.</returns>
    /// <exception cref="StudioBuilderRefused">Studio refused the request.</exception>
    public async Task<bool> Renew(string sliceId, string orderId, CancellationToken cancellationToken)
    {
        var (status, body) = await Send($"work/{sliceId}/lease", new JsonObject { ["orderId"] = orderId }, true, cancellationToken);
        if (status == HttpStatusCode.Gone) return false;
        return status == HttpStatusCode.OK ? true : throw StudioBuilderRefused.From(status, body);
    }

    /// <summary>
    /// Reports the outcome of a build.
    /// </summary>
    /// <param name="sliceId">The slice.</param>
    /// <param name="outcome">The outcome.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>How Studio settled the slice.</returns>
    /// <exception cref="StudioBuilderRefused">Studio refused the outcome.</exception>
    public async Task<string> Report(string sliceId, JsonObject outcome, CancellationToken cancellationToken)
    {
        var (status, body) = await Send($"work/{sliceId}/outcome", outcome, true, cancellationToken);
        if (status != HttpStatusCode.OK) throw StudioBuilderRefused.From(status, body);
        return Parse(body)["settled"]?.GetValue<string>() ?? outcome["kind"]!.GetValue<string>();
    }

    /// <summary>
    /// Says goodbye, so the builder shows as offline until it connects again.
    /// </summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>A task that completes when Studio answered.</returns>
    public async Task Disconnect(CancellationToken cancellationToken) => await Send("disconnect", null, true, cancellationToken);

    static JsonObject Parse(string body) => JsonNode.Parse(body) as JsonObject ?? throw new JsonException("Studio's answer is not a JSON object.");

    static string Text(JsonNode? node) => node?.GetValue<string>() ?? string.Empty;

    static int Number(JsonNode? node, int fallback) => node is JsonValue value && value.TryGetValue<int>(out var number) ? number : fallback;

    async Task<(HttpStatusCode Status, string Body)> Send(string path, JsonObject? body, bool authenticated, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Address, path));
        if (authenticated)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _credential ?? throw new InvalidOperationException("The builder has no credential."));
        }

        request.Content = new StringContent(body?.ToJsonString() ?? "{}", Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }
}
