// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_InstalledTemplates.when_reading;

public class and_a_set_was_written : given.a_templates_root
{
    InstalledTemplates _written;
    InstalledTemplates? _result;

    void Establish()
    {
        _written = new("1.8.0", ["Cratis.Templates", "Cratis.Templates.Java"], "3.0.0", DateTimeOffset.Parse("2025-01-02T03:04:05+00:00"));
        _written.Write(_root);
    }

    void Because() => _result = InstalledTemplates.Read(_root);

    [Fact] void should_read_the_version() => _result!.Version.ShouldEqual("1.8.0");
    [Fact] void should_read_the_packages() => _result!.Packages.ShouldContainOnly("Cratis.Templates", "Cratis.Templates.Java");
    [Fact] void should_read_the_minimum_cli_version() => _result!.MinimumCliVersion.ShouldEqual("3.0.0");
    [Fact] void should_leave_no_temporary_files() => Directory.GetFiles(_root).Length.ShouldEqual(1);
}
