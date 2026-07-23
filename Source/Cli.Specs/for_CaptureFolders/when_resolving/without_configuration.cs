// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CaptureFolders.when_resolving;

public class without_configuration : Specification
{
    string _result;

    void Because() => _result = CaptureFolders.Resolve(null, null, null, "/work");

    [Fact] void should_use_the_current_directory() => _result.ShouldEqual("/work");
}
