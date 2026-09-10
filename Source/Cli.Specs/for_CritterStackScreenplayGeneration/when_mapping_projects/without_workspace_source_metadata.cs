// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;

namespace Cratis.Cli.for_CritterStackScreenplayGeneration.when_mapping_projects;

public class without_workspace_source_metadata : given.a_marten_application_built_from_source
{
    IReadOnlyList<DotNetProjectCompilation> _result;

    void Because() => _result = ScreenplayProjectCompilations.From(Loaded, "/workspace/Application.slnx");

    [Fact] void should_keep_the_target_as_the_legacy_project_path() => _result[0].ProjectPath.ShouldEqual("/workspace/Application.slnx");
    [Fact] void should_keep_the_legacy_source_root() => _result[0].SourceRoot.ShouldEqual("/workspace");
    [Fact] void should_not_invent_a_source_context() => _result[0].SourceContext.ShouldBeNull();
}
