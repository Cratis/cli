// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation;

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_generating;

public class with_duplicate_project_names_and_qualified_subjects : given.an_application_scope
{
    string[] _forwardSubjects = [];
    string[] _reversedSubjects = [];

    void Because()
    {
        const string salesConcept = "namespace Sales { [Vogen.ValueObject<System.Guid>] public partial struct OrderId; }";
        const string supportConcept = "namespace Support { [Vogen.ValueObject<System.Guid>] public partial struct OrderId; }";
        var sales = Project("Shared", VogenMartenPackages, true, MartenSource, salesConcept);
        var support = Project("Shared", [], true, supportConcept);

        _forwardSubjects = SubjectsFrom(GenerateWithCritterStackFacade(LoadedFrom(sales, support)));
        _reversedSubjects = SubjectsFrom(GenerateWithCritterStackFacade(LoadedFrom(support, sales)));
    }

    /// <summary>
    /// Freezes the repeated Shared segment as legacy complete-provider qualification, not the desired atomic-roster shape.
    /// </summary>
    [Fact] void should_repeat_the_duplicate_project_name_in_each_qualified_subject_as_current_legacy_behavior() => _forwardSubjects.ShouldContainOnly(["dotnet://Shared/Shared/Sales.OrderId", "dotnet://Shared/Shared/Support.OrderId"]);
    [Fact] void should_resolve_the_same_subjects_when_duplicate_named_projects_are_reversed() => _reversedSubjects.ShouldContainOnly(_forwardSubjects);

    static string[] SubjectsFrom(GeneratedScreenplayDefinition result) =>
    [
        .. result.Graph.ConceptRepresentations
            .Select(representation => representation.Concept.Value)
            .Order(StringComparer.Ordinal)
    ];
}
