// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Cratis.Environments.for_ProjectEnvironment.given;

public sealed class VaultStorage : HttpMessageHandler
{
    public Dictionary<string, string> Values { get; } = [];

    public List<string> Deletes { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ns = request.Headers.TryGetValues("X-Vault-Namespace", out var namespaces) ? namespaces.Single() : string.Empty;
        var location = $"{ns}|{request.RequestUri!.AbsoluteUri.Replace("/metadata/", "/data/", StringComparison.Ordinal)}";
        if (request.Method == HttpMethod.Put)
        {
            using var body = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            Values[location] = body.RootElement.GetProperty("data").GetProperty("value").GetString()!;
            return new(HttpStatusCode.OK);
        }

        if (request.Method == HttpMethod.Delete)
        {
            Deletes.Add(location);
            Values.Remove(location);
            return new(HttpStatusCode.NoContent);
        }

        return Values.TryGetValue(location, out var value)
            ? new(HttpStatusCode.OK) { Content = JsonContent.Create(new { data = new { data = new { value } } }) }
            : new(HttpStatusCode.NotFound);
    }
}
