// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_an_authenticated_model : given.an_authenticated_model
{
    int _result;
    string _runDirectory;

    async Task Establish()
    {
        await CreateEnvironment();
        DockerThatCannotRun();
    }

    async Task Because()
    {
        _result = await Run(_folder, "--tag", "1.2.0");
        var mount = DockerArguments.First(_ => _.EndsWith(":/run/cratis/secrets:ro", StringComparison.Ordinal));
        _runDirectory = Directory.GetParent(Directory.GetParent(mount[..mount.IndexOf(':', StringComparison.Ordinal)]).FullName).FullName;
    }

    [Unix.Fact] void should_create_a_network_before_anything_else() => DockerArguments.Take(3).ToArray().ShouldEqual(["network", "create", DockerArguments[2]]);
    [Unix.Fact] void should_name_the_network_after_the_run() => DockerArguments[2].StartsWith("cratis-run-", StringComparison.Ordinal).ShouldBeTrue();
    [Unix.Fact] void should_start_the_stage_on_the_network() => DockerArguments.ShouldContain("--network-alias");
    [Unix.Fact] void should_publish_the_stage_on_loopback_only() => DockerArguments.ShouldContain("127.0.0.1:9090:9090");
    [Unix.Fact] void should_hand_the_environment_name_to_the_stage() => DockerArguments.ShouldContain("CRATIS_ENVIRONMENT=development");
    [Unix.Fact] void should_never_put_the_secret_in_an_argument() => DockerArguments.Any(_ => _.Contains(ClientSecret, StringComparison.Ordinal)).ShouldBeFalse();
    [Unix.Fact] void should_never_print_the_secret() => _error.ToString().ShouldNotContain(ClientSecret);
    [Unix.Fact] void should_report_that_the_stage_stopped_before_it_was_ready() => _result.ShouldEqual(ExitCodes.ServerError);
    [Unix.Fact] void should_remove_the_network_when_it_is_over() => DockerArguments.Zip(DockerArguments.Skip(1)).ShouldContain(("network", "rm"));
    [Unix.Fact] void should_delete_the_files_that_held_the_secret() => Directory.Exists(_runDirectory).ShouldBeFalse();
}
