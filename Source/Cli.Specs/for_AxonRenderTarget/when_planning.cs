// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Cli.for_AxonRenderTarget;

public class when_planning : given.an_axon_target
{
    ArtifactRenderPlan _result = null!;
    readonly ImmutableDictionary<string, string> _contents = ImmutableDictionary<string, string>.Empty.Add("Rule.java", "return true;");

    void Because() => _result = _target.Plan(_compilation, _execution, "accounts", "com.acme.accounts", _requirements, _contents, _contexts, [], _warnings, fileHeader: ["Copyright Acme"]);

    [Fact] void should_return_the_renderers_plan() => _result.ShouldEqual(_plan);
    [Fact] void should_map_the_project_name() => _input.ProjectName.ShouldEqual("accounts");
    [Fact] void should_map_the_java_package() => _input.JavaPackage.ShouldEqual("com.acme.accounts");
    [Fact] void should_pass_the_header() => _input.FileHeader.SequenceEqual(["Copyright Acme"]).ShouldBeTrue();
    [Fact] void should_pass_the_model() => _input.Model.ShouldEqual(_compilation.Model);
    [Fact] void should_pass_the_execution_plan() => _input.ExecutionPlan.ShouldEqual(_execution);
    [Fact] void should_pass_requirements() => _input.ImplementationRequirements.ShouldEqual(_requirements);
    [Fact] void should_pass_implementation_contents() => _input.ImplementationContents.ShouldEqual(_contents);
    [Fact] void should_pass_typed_contexts() => _input.TypedContextDescriptors.ShouldEqual(_contexts);
    [Fact] void should_default_to_application_scope() => _input.Scope.Kind.ShouldEqual(ArtifactRenderScopeKind.Application);
    [Fact] void should_default_the_baseline() => _input.ReleaseBaseline.ShouldEqual(ReleaseBaselineAssessment.NotSupplied);
    [Fact] void should_not_use_strict_mode() => _input.Strict.ShouldBeFalse();
    [Fact] void should_not_warn_about_absent_optional_inputs() => _warnings.Count(warning => warning.Code == "CLI-RENDER-030").ShouldEqual(0);
}
