// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_sample_data_for_a_file : given.a_model_with_sample_data
{
    void Establish() => File.WriteAllText(Path.Combine(_folder, "unrelated.play"), "not a screenplay\n");

    async Task Because() => await Run(_path, "--sample-data", "Demo");

    [Unix.Fact] void should_seed_the_explicit_pack() => File.ReadAllText(_capturedSeed).ShouldContain("Sample data demo");
    [Unix.Fact] void should_mount_only_the_selected_file_read_only() => DockerArguments.ShouldContain($"{_path}:/eventmodel/model/input.play:ro");
    [Unix.Fact] void should_not_mount_the_parent_folder_or_its_siblings() => string.Join('\n', DockerArguments).ShouldNotContain($"{_folder}:");
    [Unix.Fact] void should_compile_the_common_parent_instead_of_only_the_input_file() => DockerArguments[^1].ShouldEqual($"{StageContainer.Image}:{StageContainer.DefaultTag}");
    [Unix.Fact] void should_leave_the_selected_file_unchanged() => File.ReadAllText(_path).ShouldEqual(_source);
}
