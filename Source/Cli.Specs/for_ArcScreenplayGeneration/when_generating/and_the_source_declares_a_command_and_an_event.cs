// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ArcScreenplayGeneration.when_generating;

public class and_the_source_declares_a_command_and_an_event : given.an_application_built_from_source
{
    GeneratedScreenplay _result;

    void Because() => _result = ArcScreenplayGeneration.GenerateFrom(Loaded, $"{ProjectName}.csproj", ScreenplayGenerationOptions.Default);

    [Fact] void should_say_which_project_it_read() => _result.Projects.ShouldContainOnly([ProjectName]);
    [Fact] void should_arrange_the_namespace_into_a_feature_and_a_slice() => _result.Source.ShouldContain("feature Lending");
    [Fact] void should_describe_the_command() => _result.Source.ShouldContain("command ReserveBook");
    [Fact] void should_describe_the_event() => _result.Source.ShouldContain("event BookReserved");
    [Fact] void should_not_report_anything() => _result.Diagnostics.ShouldBeEmpty();
}
