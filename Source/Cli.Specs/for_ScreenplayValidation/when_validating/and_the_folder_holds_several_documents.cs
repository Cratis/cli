// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.when_validating;

public class and_the_folder_holds_several_documents : given.a_folder_with_documents
{
    ValidatedScreenplay _result;

    void Establish()
    {
        WriteDocument("MyApp.play", CommandSource);
        WriteDocument(Path.Combine("nested", "Events.play"), EventSource);
        WriteDocument("Concepts.play", ConceptsSource);
        WriteDocument("ignored.txt", InvalidSource);
    }

    void Because() => _result = _validation.Validate(_folder);

    [Fact] void should_count_every_discovered_play_file() => _result.FileCount.ShouldEqual(3);
    [Fact] void should_resolve_cross_file_events_and_concepts() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_produce_one_application() => _result.Applications.Count.ShouldEqual(1);
    [Fact] void should_merge_the_shared_module() => _result.Applications.Single().Modules.Count().ShouldEqual(1);
    [Fact] void should_merge_the_shared_feature() => _result.Applications.Single().Modules.Single().Features.Single().Slices.Count().ShouldEqual(2);
    [Fact] void should_retain_the_concept() => _result.Applications.Single().Concepts.Single().Name.ShouldEqual("BookId");
}
