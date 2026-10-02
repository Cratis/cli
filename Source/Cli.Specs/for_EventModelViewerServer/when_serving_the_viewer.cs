// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;

namespace Cratis.Cli.for_EventModelViewerServer;

/// <summary>
/// The viewer resolves every request relative to the page it was served as, so serving it from the root has to
/// answer the same relative routes the explorer an Arc application hosts answers.
/// </summary>
public class when_serving_the_viewer : given.a_served_application
{
    HttpResponseMessage _index;
    string _hierarchy;
    string _source;
    JsonDocument _model;
    HttpResponseMessage _unknown;
    HttpResponseMessage _traversal;

    async Task Because()
    {
        _index = await _client.GetAsync(string.Empty);
        _hierarchy = await _client.GetStringAsync("hierarchy");
        _source = await _client.GetStringAsync($"documents/{ProjectId}/{DocumentId}/source");
        _model = JsonDocument.Parse(await _client.GetStringAsync($"documents/{ProjectId}/{DocumentId}/model"));
        _unknown = await _client.GetAsync($"documents/{ProjectId}/Unknown/model");
        _traversal = await _client.GetAsync("assets/..%2F..%2Fsecrets.txt");
    }

    [Fact] void should_listen_on_the_loopback_interface() => _server.Address.IsLoopback.ShouldBeTrue();
    [Fact] void should_serve_the_viewer_page() => _index.Content.Headers.ContentType!.MediaType.ShouldEqual("text/html");
    [Fact] void should_not_let_the_browser_cache_answers() => _index.Headers.CacheControl!.NoStore.ShouldBeTrue();
    [Fact] void should_serve_the_hierarchy() => _hierarchy.ShouldContain($"\"id\":\"{ProjectId}\"");
    [Fact] void should_serve_the_source_of_a_document() => _source.ShouldEqual(Source);
    [Fact] void should_serve_a_model_that_compiled() => _model.RootElement.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_serve_the_board_document() => _model.RootElement.GetProperty("eventModel").GetProperty("name").GetString().ShouldEqual("Library");
    [Fact] void should_not_find_an_unknown_document() => _unknown.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    [Fact] void should_not_serve_anything_outside_the_viewer() => _traversal.StatusCode.ShouldEqual(HttpStatusCode.NotFound);
}
