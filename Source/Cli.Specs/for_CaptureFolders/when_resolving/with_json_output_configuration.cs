// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_CaptureFolders.when_resolving;

public class with_json_output_configuration : Specification
{
    PrologueConfiguration _configuration;
    string _result;

    void Establish() => _configuration = new PrologueConfiguration
    {
        Prologue = new PrologueOptions
        {
            Output = new OutputOptions
            {
                Kind = OutputKind.Json,
                Json = new JsonFileOptions { Directory = "./captures" }
            }
        }
    };

    void Because() => _result = CaptureFolders.Resolve(null, _configuration, "/config/cratis-prologue.json", "/work");

    [Fact] void should_resolve_the_configured_directory_against_the_configuration_folder() => _result.ShouldEqual(Path.Combine("/config", "captures"));
}
