// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Run;

namespace Cratis.Cli.for_StageContainer.when_building_run_arguments;

public class with_a_setting_overriding_a_generated_entry : Specification
{
    IReadOnlyList<string> _folder;
    IReadOnlyList<string> _file;

    void Because()
    {
        string[] settings = ["runtime:engine=eventmodel", "Sandbox:Users=true"];
        string[] extensions = ["-e", "Stage__Extensions__Count=1"];
        _folder = StageContainer.BuildRunArguments("/models", "1.2.3", 9090, 35000, "test", extensionArguments: extensions, stageSettings: settings);
        _file = StageContainer.BuildRunArgumentsForFile("/models/app.play", "1.2.3", 9090, 35000, "test", extensionArguments: extensions, stageSettings: settings);
    }

    [Fact] void should_leave_one_engine_entry_for_a_folder() => _folder.Count(argument => argument.StartsWith("Stage__Runtime__Engine=", StringComparison.OrdinalIgnoreCase)).ShouldEqual(1);
    [Fact] void should_keep_the_explicit_engine_for_a_folder() => _folder.ShouldContain("Stage__runtime__engine=eventmodel");
    [Fact] void should_leave_one_engine_entry_for_a_file() => _file.Count(argument => argument.StartsWith("Stage__Runtime__Engine=", StringComparison.OrdinalIgnoreCase)).ShouldEqual(1);
    [Fact] void should_keep_the_explicit_engine_for_a_file() => _file.ShouldContain("Stage__runtime__engine=eventmodel");
    [Fact] void should_keep_non_colliding_generated_entries() => _folder.ShouldContain("Stage__Extensions__Count=1");
    [Fact] void should_keep_the_image_after_the_settings() => _folder[^1].ShouldEqual("cratis/stage:1.2.3");
}
