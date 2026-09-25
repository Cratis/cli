// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayValidation.when_validating;

public class and_the_document_has_event_generations : given.a_folder_with_documents
{
    ValidatedScreenplay _result = null!;

    void Because() => _result = _validation.Validate(WriteDocument("Projects.play", string.Join('\n',
        "concept ProjectId : Uuid",
        "concept ProjectName : String",
        "module Projects",
        "  feature Registration",
        "    slice StateChange RegisterProject",
        "      event ProjectRegistered generation 1",
        "        projectId ProjectId",
        "        name ProjectName",
        "      event ProjectRegistered generation 2",
        "        name ProjectName")));

    [Fact] void should_compile_the_event_lineage() => _result.Applications.Count.ShouldEqual(1);
    [Fact] void should_not_report_a_compiler_error() => _result.Diagnostics.ShouldBeEmpty();
}
