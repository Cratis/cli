// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformScreenplayCommand.when_conforming;

[Collection(CliSpecsCollection.Name)]
public class and_code_lacks_a_model_member : given.a_conform_command
{
    void Establish() => File.WriteAllText(_model, Source + "      screen History\n");
    async Task Because() => await Execute();
    [Fact] void should_not_block_members_not_recovered() => _exitCode.ShouldEqual(0);
    [Fact] void should_list_nonblocking_shape_mismatches() => Findings("shapeMismatches").Any(finding => !finding.GetProperty("blocking").GetBoolean()).ShouldBeTrue();
}
