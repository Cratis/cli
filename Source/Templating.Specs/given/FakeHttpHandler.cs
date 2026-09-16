// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Templating.Specs;

/// <summary>
/// An HTTP handler that serves canned responses by URL, so acquisition specs run without network access.
/// </summary>
public sealed class FakeHttpHandler : HttpMessageHandler
{
    readonly Dictionary<string, Func<string>> _responses = [];
    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>
    /// Registers a canned response for a URL.
    /// </summary>
    /// <param name="url">The URL to respond to.</param>
    /// <param name="body">The response body producer.</param>
    public void Respond(string url, Func<string> body) => _responses[url] = body;

    /// <summary>
    /// Serves the NuGet service index with the given resource type.
    /// </summary>
    /// <param name="resourceType">The flat container resource type to advertise.</param>
    public void ServeServiceIndex(string resourceType) => Respond(
        "https://feed.example/v3/index.json",
        () => $$"""{ "resources": [ { "@type": "{{resourceType}}", "@id": "https://flat.example" } ] }""");

    /// <summary>
    /// Serves the flat container version list for a package.
    /// </summary>
    /// <param name="packageId">The package id (lowercased).</param>
    /// <param name="versions">The versions to advertise.</param>
    public void ServeVersions(string packageId, params string[] versions) => Respond(
        $"https://flat.example/{packageId}/index.json",
        () => $$"""{ "versions": [{{string.Join(',', versions.Select(v => $"\"{v}\""))}}] }""");

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var url = request.RequestUri!.ToString();
        if (!_responses.TryGetValue(url, out var body))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body())
        });
    }
}
