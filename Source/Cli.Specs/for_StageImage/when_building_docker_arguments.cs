// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Run;

namespace Cratis.Cli.for_StageImage;

public class when_building_docker_arguments : Specification
{
    IReadOnlyList<string> _listTagsArguments;
    IReadOnlyList<string> _pullArguments;

    void Because()
    {
        _listTagsArguments = StageImage.BuildListTagsArguments();
        _pullArguments = StageImage.BuildPullArguments("4.4.0");
    }

    [Fact] void should_list_images_for_the_stage_repository() => _listTagsArguments.ShouldContain(StageContainer.Image);
    [Fact] void should_ask_for_tags_only() => _listTagsArguments.ShouldContain("{{.Tag}}");

    [Fact] void should_pull_the_given_tag() => _pullArguments.ShouldContain($"{StageContainer.Image}:4.4.0");
}
