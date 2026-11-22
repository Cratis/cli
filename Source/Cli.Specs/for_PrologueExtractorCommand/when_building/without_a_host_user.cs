// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_PrologueExtractorCommand.when_building;

public class without_a_host_user : Specification
{
    string _result;

    void Because() => _result = PrologueExtractorCommand.Build(
        new PrologueWizardInput(Guid.NewGuid(), [], [], null, null, new(OutputKind.Json, "", "./captures")),
        "/project/cratis-prologue.json");

    [Fact] void should_not_set_a_user() => _result.ShouldNotContain("--user");
}
