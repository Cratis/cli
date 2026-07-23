// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_CaptureFolders.when_resolving;

public class with_explicit_path : Specification
{
    string _result;

    void Because() => _result = CaptureFolders.Resolve(
        "captures",
        new PrologueConfiguration(),
        "/config/cratis-prologue.json",
        "/work");

    [Fact] void should_resolve_the_path_against_the_current_directory() => _result.ShouldEqual(Path.Combine("/work", "captures"));
}
