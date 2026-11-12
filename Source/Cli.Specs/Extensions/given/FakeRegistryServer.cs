// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Cli.Extensions;

/// <summary>
/// Serves canned responses by request path, and remembers what was asked and with which credentials.
/// </summary>
sealed class FakeRegistryServer : HttpMessageHandler
{
    readonly Dictionary<string, (string MediaType, byte[] Body)> _resources = [];

    public List<string> Requests { get; } = [];

    public List<string?> Authorizations { get; } = [];

    public Func<HttpRequestMessage, HttpResponseMessage?>? Intercept { get; set; }

    public static string DigestOf(byte[] bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    public FakeRegistryServer Serve(string path, string mediaType, byte[] body)
    {
        _resources[path] = (mediaType, body);
        return this;
    }

    public FakeRegistryServer Serve(string path, string json) => Serve(path, "application/json", Encoding.UTF8.GetBytes(json));

    public bool Served(string path) => Requests.Contains(path);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.PathAndQuery;
        Requests.Add(path);
        Authorizations.Add(request.Headers.Authorization?.ToString());
        if (Intercept?.Invoke(request) is { } intercepted) return Task.FromResult(intercepted);
        if (!_resources.TryGetValue(path, out var resource)) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(resource.Body) };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(resource.MediaType);
        return Task.FromResult(response);
    }
}
