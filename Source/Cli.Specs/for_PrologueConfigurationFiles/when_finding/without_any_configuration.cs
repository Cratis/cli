// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PrologueConfigurationFiles.when_finding;

public class without_any_configuration : given.a_temporary_folder
{
    string? _result;

    void Because() => _result = PrologueConfigurationFiles.Find(null, _folder);

    [Fact] void should_not_find_a_configuration() => _result.ShouldBeNull();
}
