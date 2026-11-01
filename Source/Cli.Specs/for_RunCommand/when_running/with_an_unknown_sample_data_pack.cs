// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class with_an_unknown_sample_data_pack : given.a_model_with_sample_data
{
    int _result;

    async Task Because() => _result = await Run(_folder, "--sample-data", "missing");

    [Fact] void should_refuse_before_starting_docker() => DockerArguments.ShouldBeEmpty();
    [Fact] void should_return_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_name_the_pack() => System.Text.Json.Nodes.JsonNode.Parse(_error.ToString())!["message"]!.GetValue<string>().ShouldEqual("Sample data pack 'missing' was not found.");
}
