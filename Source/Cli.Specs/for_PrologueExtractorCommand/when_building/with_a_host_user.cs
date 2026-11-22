// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueExtractorCommand.when_building;

public class with_a_host_user : Specification
{
    string _result;
    string _receiver;

    void Because()
    {
        var json = new PrologueWizardInput(Guid.NewGuid(), [], [], null, null, new(OutputKind.Json, "", "./captures"));
        var api = new PrologueWizardInput(Guid.NewGuid(), [], [], null, null, new(OutputKind.Api, "http://localhost:5005", ""));
        _result = PrologueExtractorCommand.Build(json, "/project/cratis-prologue.json", "1000:1001");
        _receiver = PrologueExtractorCommand.Build(api, "/project/cratis-prologue.json", "1000:1001");
    }

    [Fact] void should_run_the_container_as_the_host_user() => _result.ShouldContain("docker run --rm --user 1000:1001 ");
    [Fact] void should_not_set_a_user_for_receiver_output() => _receiver.ShouldNotContain("--user");
}
