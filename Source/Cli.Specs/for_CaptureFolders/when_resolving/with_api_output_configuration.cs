// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_CaptureFolders.when_resolving;

public class with_api_output_configuration : Specification
{
    string _result;

    void Because() => _result = CaptureFolders.Resolve(
        null,
        new PrologueConfiguration { Prologue = new PrologueOptions { Output = new OutputOptions { Kind = OutputKind.Api } } },
        "/config/cratis-prologue.json",
        "/work");

    [Fact] void should_use_the_current_directory() => _result.ShouldEqual("/work");
}
