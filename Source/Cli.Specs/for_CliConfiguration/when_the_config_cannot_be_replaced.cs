// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliConfiguration;

[Collection(CliSpecsCollection.Name)]
public class when_the_config_cannot_be_replaced : given.a_temp_config_directory
{
    Exception? _error;
    string _sentinel = null!;

    void Establish()
    {
        Directory.CreateDirectory(CliConfiguration.GetConfigPath());
        _sentinel = Path.Combine(CliConfiguration.GetConfigPath(), "sentinel");
        File.WriteAllText(_sentinel, "original");
    }

    void Because() => _error = Catch.Exception(() => new CliConfiguration().Save());

    [Fact] void should_report_the_save_failure() => _error.ShouldNotBeNull();
    [Fact] void should_leave_the_existing_destination_untouched() => File.ReadAllText(_sentinel).ShouldEqual("original");
    [Fact] void should_remove_the_temporary_file() => Directory.GetFiles(Path.GetDirectoryName(CliConfiguration.GetConfigPath())!, "*.tmp").ShouldBeEmpty();
}
