// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.for_ModelRootReleaseBaseline.given;

public class a_model_root : for_ScreenplayPlanning.when_planning_with_a_release_baseline.given.a_release_baseline
{
    protected string _root = null!;
    protected string _baselinePath = null!;
    protected string _identitiesPath = null!;
    protected Func<string, ReleaseBaselineOutcome> _run = null!;
    protected ReleaseBaselineAssessment _assessment = null!;
    protected SemanticCompilation _compilation = null!;
    private protected ModelRootReleaseBaseline _check = null!;

    void Establish()
    {
        _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"cli-baseline-{Guid.NewGuid():N}")).FullName;
        var metadata = Directory.CreateDirectory(Path.Combine(_root, ".screenplay")).FullName;
        _baselinePath = Path.Combine(metadata, ReleaseBaselineStore.FileName);
        _identitiesPath = Path.Combine(metadata, "identities.json");
        File.WriteAllBytes(_baselinePath, ReleaseBaselineStore.Serialize(_baseline.Document!));
        File.WriteAllText(_identitiesPath, "persisted identities checked by the injected runner");
        _compilation = Workspace(Source, SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("TemporaryRender"))).Compilation.Value!;
        _run = Substitute.For<Func<string, ReleaseBaselineOutcome>>();
        _run(_root).Returns(new ReleaseBaselineOutcome(0, []));
        _check = new(_root, _run);
    }

    void Destroy() => Directory.Delete(_root, true);
}
