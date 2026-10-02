// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Screenplay.Embedded.Hosting;
using Cratis.Arc.Screenplay.Embedded.Hosting.Assets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Cli.Commands.View;

/// <summary>
/// Serves the event model explorer Arc ships, over the documents of one application, on a local web server.
/// </summary>
/// <remarks>
/// <para>
/// The viewer is the same application an Arc host serves at <c language="http">/.cratis/event-model/</c> - its assets come from the
/// Arc package and every answer comes from an <see cref="EventModelExplorer"/>. Only the documents differ: they are
/// the ones the CLI read from the built assembly or generated from source, served from the root of the server.
/// The viewer resolves its requests relative to the page it was served as, so it cannot tell the two hosts apart.
/// </para>
/// <para>
/// The server listens on the loopback interface only. It serves an application's model to the developer running
/// the command, not to the network that developer happens to be on.
/// </para>
/// </remarks>
public sealed class EventModelViewerServer : IAsyncDisposable
{
    readonly WebApplication _application;

    EventModelViewerServer(WebApplication application, Uri address)
    {
        _application = application;
        Address = address;
    }

    /// <summary>
    /// Gets the address the viewer is served at.
    /// </summary>
    public Uri Address { get; }

    /// <summary>
    /// Starts serving the viewer.
    /// </summary>
    /// <param name="explorer">The <see cref="EventModelExplorer"/> answering for the documents to show.</param>
    /// <param name="port">The local port to listen on; 0 picks a free one.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The started <see cref="EventModelViewerServer"/>.</returns>
    public static async Task<EventModelViewerServer> Start(EventModelExplorer explorer, int port, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { Args = [] });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, port));

        var application = builder.Build();
        Map(application, explorer, EmbeddedViewerAssets.Viewer);

        await application.StartAsync(cancellationToken);

        var address = application.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault() ??
            $"http://{IPAddress.Loopback}:{port}";

        return new EventModelViewerServer(application, new Uri($"{address.TrimEnd('/')}/"));
    }

    /// <summary>
    /// Waits until the server is stopped.
    /// </summary>
    /// <param name="cancellationToken">The token that stops it.</param>
    /// <returns>A <see cref="Task"/> that completes when the server has stopped.</returns>
    public async Task WaitForShutdown(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Stopping is how the view ends; it is not a failure.
        }

        await _application.StopAsync(CancellationToken.None);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => _application.DisposeAsync();

    /// <summary>
    /// Maps the viewer and the API it calls.
    /// </summary>
    /// <param name="endpoints">The <see cref="Microsoft.AspNetCore.Routing.IEndpointRouteBuilder"/> to map into.</param>
    /// <param name="explorer">The <see cref="EventModelExplorer"/> answering for the documents.</param>
    /// <param name="assets">The assets of the viewer.</param>
    /// <remarks>
    /// The routes are the contract the viewer is written against - relative to the page, <c language="http">hierarchy</c>,
    /// <c language="http">documents/{project}/{document}/source</c>, <c language="http">documents/{project}/{document}/model</c>
    /// and <c language="http">assets/{path}</c>. Each one only translates an answer the explorer or the assets give into HTTP.
    /// </remarks>
    internal static void Map(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, EventModelExplorer explorer, EmbeddedViewerAssets assets)
    {
        var group = endpoints.MapGroup(string.Empty);
        group.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            return await next(context);
        });

        group.MapGet("/", () =>
            assets.TryReadIndex(out var content, out var contentType)
                ? Results.File(content, contentType)
                : Results.NotFound());
        group.MapGet("/hierarchy", () => Results.Json(explorer.Hierarchy, EventModelJson.SerializerOptions));
        group.MapGet("/documents/{projectId}/{documentId}/source", (string projectId, string documentId) =>
            explorer.TryGetSource(projectId, documentId, out var source)
                ? Results.Text(source, "text/plain")
                : Results.NotFound());
        group.MapGet("/documents/{projectId}/{documentId}/model", (string projectId, string documentId) =>
            explorer.TryGetModel(projectId, documentId, out var model)
                ? Results.Json(model, EventModelJson.SerializerOptions)
                : Results.NotFound());
        group.MapGet("/assets/{**path}", (string? path) =>
            assets.TryRead($"assets/{path}", out var content, out var contentType)
                ? Results.File(content, contentType)
                : Results.NotFound());
    }
}
