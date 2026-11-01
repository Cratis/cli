// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class without_opting_into_sample_data : given.a_model_with_sample_data
{
    async Task Because() => await Run(_folder);

    [Unix.Fact] void should_keep_mounting_the_original_folder() => DockerArguments.ShouldContain($"{_folder}:/eventmodel:ro");
    [Unix.Fact] void should_not_mount_seed_events() => File.Exists(_capturedSeed).ShouldBeFalse();
    [Unix.Fact] void should_leave_the_source_unchanged() => File.ReadAllText(_path).ShouldEqual(_source);
}
