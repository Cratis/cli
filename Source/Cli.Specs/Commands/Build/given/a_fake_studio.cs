// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace Cratis.Cli.Commands.Build.given;

/// <summary>
/// Answers builder requests the way Studio's builder connection endpoints do, from a queue of answers per path.
/// </summary>
public sealed class a_fake_studio : HttpMessageHandler
{
    readonly ConcurrentDictionary<string, ConcurrentQueue<(HttpStatusCode Status, string Body)>> _answers = new();

    public ConcurrentQueue<(string Path, string? Authorization, string Body)> Requests { get; } = new();

    public void Answer(string path, HttpStatusCode status, string body = "") =>
        _answers.GetOrAdd(path, _ => new()).Enqueue((status, body));

    public IEnumerable<string> BodiesTo(string path) => Requests.Where(request => request.Path == path).Select(request => request.Body);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath.Replace("/builders/connect/acme/", string.Empty, StringComparison.Ordinal);
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Enqueue((path, request.Headers.Authorization?.ToString(), body));
        var fallback = path == "work" ? (HttpStatusCode.NoContent, string.Empty) : (HttpStatusCode.OK, "{}");
        var (status, answer) = _answers.TryGetValue(path, out var queue) && queue.TryDequeue(out var next) ? next : fallback;
        return new HttpResponseMessage(status) { Content = new StringContent(answer, Encoding.UTF8, "application/json") };
    }
}
