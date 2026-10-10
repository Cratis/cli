// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayConformance;

public class when_classifying_dependants_and_identities : given.a_structural_comparison
{
    void Establish() => _after += "        extra String\n";
    void Because() => Compare();
    [Fact] void should_have_real_dependant_context() => _difference.Dependants.ShouldNotBeEmpty();
    [Fact] void should_not_classify_dependants_as_findings() => _findings.Count.ShouldEqual(_difference.Declarations.Count + _difference.Events.Count + _difference.Members.Count + _difference.Specifications.Count);
    [Fact] void should_ignore_identity_continuity_in_address_mode() => _difference.Identities.ShouldBeEmpty();
}
