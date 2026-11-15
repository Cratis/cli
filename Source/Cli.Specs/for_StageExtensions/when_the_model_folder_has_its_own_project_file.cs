// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run.for_StageExtensions;

public class when_the_model_folder_has_its_own_project_file : given.a_plan
{
    string _line;

    void Establish() => Create(replacesModelProjectFile: true);

    void Because() => _line = string.Join(' ', Plan.BuildArguments(new("/run/secrets.env", "/run/extensions.json"), "/eventmodel/model"));

    [Fact] void should_replace_it_in_the_container_with_the_allow_list_in_the_shape_stage_reads() => _line.ShouldContain("-v /run/extensions.json:/eventmodel/model/.cratis/extensions.json:ro");
}
