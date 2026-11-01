// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageContainer.when_building_run_arguments;

public class with_runtime_options : Specification
{
    IReadOnlyList<string> _folder;
    IReadOnlyList<string> _file;

    void Because()
    {
        var runtime = new StageRuntimeOptions("cratis-run-1", "stage", "127.0.0.1", [new("CRATIS_ENVIRONMENT", "development")], [new("/run/dir/secrets", "/run/cratis/secrets")]);
        _folder = StageContainer.BuildRunArguments("/work/space", "1.2.0", 9090, 35000, "cratis-stage-abc123", runtime: runtime);
        _file = StageContainer.BuildRunArgumentsForFile("/work/space/a.play", "1.2.0", 9090, 35000, "cratis-stage-abc123", runtime: runtime);
    }

    [Fact] void should_join_the_network_for_a_folder() => _folder.Zip(_folder.Skip(1)).ShouldContain(("--network", "cratis-run-1"));
    [Fact] void should_join_the_network_for_a_file() => _file.Zip(_file.Skip(1)).ShouldContain(("--network", "cratis-run-1"));
    [Fact] void should_be_reachable_by_its_alias() => _folder.Zip(_folder.Skip(1)).ShouldContain(("--network-alias", "stage"));
    [Fact] void should_publish_the_api_on_the_given_address_only() => _folder.ShouldContain("127.0.0.1:9090:9090");
    [Fact] void should_publish_the_workbench_on_the_given_address_only() => _file.ShouldContain("127.0.0.1:35000:35000");
    [Fact] void should_pass_the_environment() => _folder.ShouldContain("CRATIS_ENVIRONMENT=development");
    [Fact] void should_mount_the_secrets_read_only() => _file.ShouldContain("/run/dir/secrets:/run/cratis/secrets:ro");
    [Fact] void should_still_end_with_the_image_for_a_folder() => _folder[^1].ShouldEqual("cratis/stage:1.2.0");
    [Fact] void should_still_pass_the_file_after_the_image() => _file[^1].ShouldEqual("/eventmodel/input.play");
}
