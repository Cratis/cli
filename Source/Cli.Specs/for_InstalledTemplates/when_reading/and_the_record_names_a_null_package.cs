// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_InstalledTemplates.when_reading;

public class and_the_record_names_a_null_package : given.a_templates_root
{
    InstalledTemplates? _result;

    void Establish() => File.WriteAllText(Path.Combine(_root, InstalledTemplates.FileName), """{ "version": "1.8.0", "packages": [null, "Cratis.Templates"], "installedAt": "not a date" }""");

    void Because() => _result = InstalledTemplates.Read(_root);

    [Fact] void should_not_be_read() => _result.ShouldBeNull();
}
