// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_with_a_release_baseline.given;

public class a_release_baseline : Specification
{
    protected const string Source = "concept RecordId : Uuid\nmodule Records\n  feature Recording\n    slice StateChange Record\n      command Record\n        recordId RecordId identifier\n        value String\n        produces Recorded\n          for recordId\n          value = value\n      event Recorded\n        value String\n    slice StateChange Observe\n      command Observe\n        recordId RecordId identifier\n        produces Removed\n          for recordId\n      event Removed\n";
    protected ScreenplayWorkspace _workspace = null!;
    protected ReleaseBaselineRead _baseline = null!;
    private protected ScreenplayDocumentRenderRequest _request = null!;
    private protected ScreenplayRenderPlan _result = null!;
    private protected ScreenplayRenderPlan _withoutBaseline = null!;

    void Establish()
    {
        var bootstrap = Workspace(Source, SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("RecordsApp")));
        _workspace = Workspace(Source, bootstrap.IdentityCatalog);
        var admitted = ReleaseBaseline.Admit(_workspace, ReleaseBaselineStore.ReadBytes(null, _workspace.IdentityCatalog.Application), "v1.0", DateTimeOffset.UnixEpoch);
        _baseline = ReleaseBaselineStore.ReadBytes(ReleaseBaselineStore.Serialize(admitted.Document!), _workspace.IdentityCatalog.Application);
        _request = Request(_workspace) with { ReleaseBaseline = new FileReleaseBaseline(_baseline) };
    }

    protected static ScreenplayWorkspace Workspace(string source, SemanticIdentityCatalog catalog) => ScreenplayWorkspace.Create(
        catalog.Application,
        "RecordsApp",
        [WorkspaceDocument.Create(catalog.ResolveDocument("source"), "source", PortablePlayPath.Parse("source.play"), Encoding.UTF8.GetBytes(source))],
        catalog);

    private protected static ScreenplayDocumentRenderRequest Request(ScreenplayWorkspace workspace) => new(
        SemanticDocumentSet.Create([.. workspace.Documents.Select(document => SemanticSourceDocument.Create(document.Id, document.StableKey, document.Path.Value, document.Text))], workspace.IdentityCatalog),
        workspace.ApplicationName,
        "cratis");

    private protected ScreenplayDocumentRenderRequest Changed(string source) => Request(Workspace(source, _workspace.IdentityCatalog)) with { ReleaseBaseline = new FileReleaseBaseline(_baseline) };

    private protected void Plan()
    {
        var planning = new ScreenplayPlanning();
        _result = planning.PlanDocuments(_request, CancellationToken.None);
        _withoutBaseline = planning.PlanDocuments(_request with { ReleaseBaseline = null }, CancellationToken.None);
    }
}
