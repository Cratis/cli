// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Fixture = Cratis.Cli.for_ScreenplayPlanning.when_carrying_semantic_exclusions.given.an_excluded_slice;

namespace Cratis.Cli.for_RenderSelection;

public class when_filtering_semantic_exclusions : Specification
{
    SemanticAddressIndex _index = null!;
    SemanticExclusion _slice = null!;
    SemanticExclusion _specification = null!;
    SemanticExclusion _nested = null!;
    SemanticExclusion _sibling = null!;
    ImmutableArray<SemanticExclusion> _exclusions;

    void Establish()
    {
        const string source = Fixture.Source + "    feature Nested\n      slice StateChange Keep\n        event Kept\n          value String\n  feature RegistrationExtra\n    slice StateChange Other\n      event OtherKept\n        value String\n";
        var compiled = new SemanticModelCompiler().CompileAdmissible("Projects", Fixture.Documents(source));
        compiled.Compilation.Success.ShouldBeTrue();
        _index = SemanticAddressIndex.From(compiled.Compilation.Value.Model.Application);
        _slice = compiled.Exclusions.Single();
        _specification = _slice with { Kind = SemanticExclusionKind.Specification, Address = "Projects/Registration/RegisterProject/Rejected" };
        _nested = _slice with { Address = "Projects/Registration/Nested/Removed", DependsOn = [new(SemanticExclusionKind.Slice, "Projects/RegistrationExtra/Removed")] };
        _sibling = _slice with { Address = "Projects/RegistrationExtra/Removed" };
        _exclusions = [_slice, _specification, _nested, _sibling, _slice with { Kind = SemanticExclusionKind.Import, Address = "Projects/Registration/Import" }];
    }

    [Fact] void should_include_the_module_subtree() => Select("Projects").ShouldEqual(_exclusions.Take(4));
    [Fact] void should_include_nested_features_but_not_same_prefix_siblings() => Select("Projects.Registration").ShouldEqual(new[] { _slice, _specification, _nested });
    [Fact] void should_include_only_the_selected_nested_subtree() => Select("Projects.Registration.Nested").ShouldEqual(new[] { _nested });
    [Fact] void should_include_specification_evidence_of_a_surviving_selected_slice() => Select("Projects.Registration.RegisterProject").ShouldEqual(new[] { _specification });
    [Fact] void should_union_overlapping_scopes_without_duplicates() => Select("Projects", "Projects.Registration").ShouldEqual(_exclusions.Take(4));
    [Fact] void should_preserve_dependencies_outside_the_scope() => Select("Projects.Registration.Nested").Single().ShouldBeSame(_nested);
    [Fact]
    void should_not_invent_a_directly_selected_removed_slice()
    {
        var diagnostics = new List<ScreenplayDiagnostic>();
        RenderSelection.Resolve(["Projects.Registration.Removed"], _index, diagnostics).ShouldBeNull();
        diagnostics.Single().Code.ShouldEqual("CLI-RENDER-011");
    }
    [Fact] void should_normalize_absent_metadata() => Selection("Projects").SelectExclusions(default, _index).IsDefault.ShouldBeFalse();

    ImmutableArray<SemanticExclusion> Select(params string[] scopes) => Selection(scopes).SelectExclusions(_exclusions, _index);
    RenderSelection Selection(params string[] scopes) => RenderSelection.Resolve(scopes, _index, [])!;
}
