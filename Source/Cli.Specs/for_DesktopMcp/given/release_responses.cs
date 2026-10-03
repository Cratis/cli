// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;

namespace Cratis.Cli.for_DesktopMcp.given;

/// <summary>Supplies publisher responses without reaching a network.</summary>
/// <param name="respond">Response for a recorded request.</param>
public class release_responses(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
}
