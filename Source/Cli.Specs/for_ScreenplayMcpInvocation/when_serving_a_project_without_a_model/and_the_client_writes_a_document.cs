// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ScreenplayMcpInvocation.when_serving_a_project_without_a_model;

public class and_the_client_writes_a_document : given.a_live_mcp_session
{
#pragma warning disable MA0136 // The Screenplay compiler accepts either line ending in model source.
    const string Source = """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId Uuid identifier
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
              event ProjectRegistered
                projectId Uuid
        """;
#pragma warning restore MA0136

    bool _existedBeforeApply;
    JsonElement _applied;

    async Task Because()
    {
        var opened = Call("open-workspace").GetProperty("structuredContent");
        var proposed = Call("propose-source", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            formatting = "CanonicalizeTouchedDocuments",
            documents = new[] { new { operation = "create-document", path = "projects.play", stableKey = "projects", source = Source } }
        }).GetProperty("structuredContent");
        _existedBeforeApply = Directory.Exists(ModelFolder);
        _applied = Call("apply", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            proposalId = proposed.GetProperty("proposalId").GetString()
        });
        await Stop();
    }

    [Fact] void should_not_create_the_folder_for_a_proposal() => _existedBeforeApply.ShouldBeFalse();
    [Fact] void should_apply_the_document() => _applied.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_create_the_model_folder_on_the_first_write() => Directory.Exists(ModelFolder).ShouldBeTrue();
    [Fact] void should_write_the_document_into_it() => File.Exists(Path.Combine(ModelFolder, "projects.play")).ShouldBeTrue();
}
