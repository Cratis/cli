// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning.given;

public class a_failing_render_target : a_screenplay_planning
{
    protected Exception _failure = new ArgumentException("Unexpected renderer failure.");
    protected Exception? _exception;
    private protected ScreenplayDocumentRenderRequest _request = null!;

    void Establish()
    {
        var target = Substitute.For<IRenderTarget>();
        target.Name.Returns("failing");
        target.UsesCratisConventions.Returns(true);
        target.NewestAdmittedSemanticVersion.Returns(RenderedSemanticVersions.Newest);
        target.Plan(default!, default!, null, null, default, default!, default, default, default!)
            .ReturnsForAnyArgs(_ => throw _failure);
        _planning = new(new SemanticModelCompiler(), new RenderTargetRoster([target]));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "Source.play", Source);
        _request = new(SemanticDocumentSet.Create([document], catalog), "Projects", "failing", null, null);
    }
}
