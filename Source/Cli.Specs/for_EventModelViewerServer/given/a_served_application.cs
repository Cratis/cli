// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Embedded.Hosting;
using Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

namespace Cratis.Cli.for_EventModelViewerServer.given;

#pragma warning disable MA0136 // The fixture is normalized to LF before it is served.

/// <summary>
/// A viewer served on a free local port over one in-memory document, the way 'cratis view' serves documents it
/// generated from source.
/// </summary>
public class a_served_application : Specification
{
    protected const string ProjectId = "Library";
    protected const string DocumentId = "Library";

    protected static readonly string Source = """
        domain Library

        module Catalog
          feature Books
            slice StateChange RegisterBook
              command RegisterBook
                bookId Uuid identifier
                title  String

              event BookRegistered
                bookId Uuid
                title  String

        """.ReplaceLineEndings("\n");

    protected EventModelViewerServer _server;
    protected HttpClient _client;

    async Task Establish()
    {
        var resourceName = EmbeddedResourceNames.ForDocument(DocumentId);
        var catalog = new EmbeddedDocumentCatalog([new EmbeddedDocument(DocumentId, ProjectId, DocumentId, EmbeddedDocumentKind.Assembly, null, resourceName)]);
        var resources = new InMemoryEventModelResources(ProjectId, new Dictionary<string, string>
        {
            [EmbeddedResourceNames.Catalog] = catalog.Serialize(),
            [resourceName] = Source
        });

        _server = await EventModelViewerServer.Start(new EventModelExplorer(EventModelCatalog.For([resources])), 0, CancellationToken.None);
        _client = new HttpClient { BaseAddress = _server.Address };
    }

    async Task Destroy()
    {
        _client.Dispose();
        await _server.DisposeAsync();
    }
}
