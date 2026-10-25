// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_sample_data : given.a_model_with_sample_data
{
    async Task Because() => await Run(_folder, "--sample-data");

    [Unix.Fact] void should_seed_the_active_pack() => File.ReadAllText(_capturedSeed).ShouldContain("customer = \"Sample customer that must never ship\"");
    [Unix.Fact] void should_mount_the_original_model_read_only() => DockerArguments.ShouldContain($"{_folder}:/eventmodel/model:ro");
    [Unix.Fact] void should_mount_the_seed_read_only_beside_the_model() => DockerArguments.ShouldContain(argument => argument.EndsWith(":/eventmodel/cratis-sample-data.play:ro", StringComparison.Ordinal));
    [Unix.Fact] void should_remove_the_seed_folder_after_the_container_fails() => Directory.Exists(Path.GetDirectoryName(DockerArguments.Single(argument => argument.EndsWith(":/eventmodel/cratis-sample-data.play:ro", StringComparison.Ordinal))[..^":/eventmodel/cratis-sample-data.play:ro".Length])).ShouldBeFalse();
    [Unix.Fact] void should_leave_the_source_unchanged() => File.ReadAllText(_path).ShouldEqual(_source);
    [Unix.Fact] void should_not_add_a_seed_to_the_source_folder() => Directory.GetFiles(_folder).Length.ShouldEqual(1);
}
