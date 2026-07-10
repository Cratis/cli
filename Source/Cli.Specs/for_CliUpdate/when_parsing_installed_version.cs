// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliUpdate;

public class when_parsing_installed_version : Specification
{
    [Fact]
    void should_find_version_in_dotnet_tool_list_output()
    {
        var stdout =
            "Package Id      Version      Commands\n" +
            "-------------------------------------\n" +
            "cratis.cli      2.0.1        cratis\n";

        var version = CliUpdate.ParseDotNetToolListVersion(stdout);
        version.ShouldEqual("2.0.1");
    }

    [Fact]
    void should_return_null_when_package_not_in_dotnet_tool_list_output()
    {
        var stdout =
            "Package Id      Version      Commands\n" +
            "-------------------------------------\n" +
            "some.other.tool 1.0.0        other\n";

        var version = CliUpdate.ParseDotNetToolListVersion(stdout);
        version.ShouldBeNull();
    }

    [Fact]
    void should_find_latest_version_in_brew_list_output_with_single_version()
    {
        var version = CliUpdate.ParseBrewListVersion("cratis 2.0.1\n");
        version.ShouldEqual("2.0.1");
    }

    [Fact]
    void should_find_latest_version_in_brew_list_output_with_multiple_versions()
    {
        var version = CliUpdate.ParseBrewListVersion("cratis 1.6.3 2.0.1\n");
        version.ShouldEqual("2.0.1");
    }

    [Fact]
    void should_return_null_for_empty_brew_list_output()
    {
        var version = CliUpdate.ParseBrewListVersion(string.Empty);
        version.ShouldBeNull();
    }
}
