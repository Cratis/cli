// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_JobCommands.when_deleting;

public class and_the_operator_is_asked : given.a_delete_job_command
{
    string _prompt;

    void Because() => _prompt = _command.Confirmation(_settings);

    [Fact] void should_name_the_event_store() => _prompt.ShouldContain(_settings.EventStore!);
    [Fact] void should_name_the_namespace() => _prompt.ShouldContain(_settings.Namespace);
    [Fact] void should_name_the_job() => _prompt.ShouldContain(_settings.JobId);
    [Fact] void should_describe_the_retained_state_removal() => _prompt.ShouldContain("The retained job and its state are removed");
    [Fact] void should_say_it_cannot_be_undone() => _prompt.ShouldContain("cannot be undone");
}
