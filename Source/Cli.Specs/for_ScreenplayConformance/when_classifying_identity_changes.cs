// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;

namespace Cratis.Cli.for_ScreenplayConformance;

public class when_classifying_identity_changes : Specification
{
    ModelDifference _difference = null!;
    IReadOnlyList<ConformanceFinding> _findings = [];

    void Because()
    {
        const string source = "module Library\n  feature Registration\n    slice StateChange Register\n      event Registered\n        name String\n";
        var before = ComparedModel.FromSources("Library", new Dictionary<string, string> { ["application.play"] = source });
        var after = ComparedModel.FromSources("Library", new Dictionary<string, string> { ["application.play"] = source + "        extra String\n" });
        _difference = ModelComparison.Compare(ComparedModel.WithIdentities(before.Workspace), ComparedModel.WithIdentities(after.Workspace));
        _findings = ScreenplayConformance.Classify(_difference);
    }

    [Fact] void should_have_real_identity_changes_to_ignore() => _difference.Identities.ShouldNotBeEmpty();
    [Fact] void should_not_add_identity_findings() => _findings.Count.ShouldEqual(_difference.Declarations.Count + _difference.Events.Count + _difference.Members.Count + _difference.Specifications.Count);
}
