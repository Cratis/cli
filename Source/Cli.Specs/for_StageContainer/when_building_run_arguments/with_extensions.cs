// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageContainer.when_building_run_arguments;

public class with_extensions : Specification
{
    IReadOnlyList<string> _arguments;
    IReadOnlyList<string> _fileArguments;

    void Because()
    {
        IReadOnlyList<string> extensions = ["-e", "Stage__Extensions__HostPort=50100", "--env-file", "/run/secrets.env"];
        _arguments = StageContainer.BuildRunArguments("/work/space", "1.2.0", 9090, 35000, "cratis-stage-abc123", extensionArguments: extensions);
        _fileArguments = StageContainer.BuildRunArgumentsForFile("/work/space/model.play", "1.2.0", 9090, 35000, "cratis-stage-abc123", extensionArguments: extensions);
    }

    [Fact] void should_hand_over_the_environment_file() => _arguments.ShouldContain("/run/secrets.env");
    [Fact] void should_put_the_extension_arguments_before_the_image() => _arguments.ToList().IndexOf("--env-file").ShouldBeLessThan(_arguments.ToList().IndexOf("cratis/stage:1.2.0"));
    [Fact] void should_put_them_before_the_image_for_a_file_too() => _fileArguments.ToList().IndexOf("--env-file").ShouldBeLessThan(_fileArguments.ToList().IndexOf("cratis/stage:1.2.0"));
}
