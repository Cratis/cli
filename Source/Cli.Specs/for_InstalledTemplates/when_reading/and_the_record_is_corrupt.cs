// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_InstalledTemplates.when_reading;

public class and_the_record_is_corrupt : given.a_templates_root
{
    InstalledTemplates? _result;

    void Establish() => File.WriteAllText(Path.Combine(_root, InstalledTemplates.FileName), "{ \"version\": ");

    void Because() => _result = InstalledTemplates.Read(_root);

    [Fact] void should_treat_nothing_as_installed() => _result.ShouldBeNull();
}
