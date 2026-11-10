// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildServicePlatforms;

public class when_finding_the_program_of_a_service : Specification
{
    (string Program, IReadOnlyList<string> Prefix) _tool;
    (string Program, IReadOnlyList<string> Prefix) _host;

    void Because()
    {
        _tool = BuildServicePlatforms.Program("/Users/someone/.dotnet/tools/cratis", "/somewhere/Cratis.Cli.dll");
        _host = BuildServicePlatforms.Program("/usr/local/share/dotnet/dotnet", "/somewhere/Cratis.Cli.dll");
    }

    [Fact] void should_start_the_installed_tool_itself() => _tool.Program.ShouldEqual("/Users/someone/.dotnet/tools/cratis");
    [Fact] void should_pass_nothing_before_the_tool_s_arguments() => _tool.Prefix.ShouldBeEmpty();
    [Fact] void should_start_the_host_when_run_through_dotnet() => _host.Program.ShouldEqual("/usr/local/share/dotnet/dotnet");
    [Fact] void should_pass_the_assembly_to_the_host() => _host.Prefix.ToArray().ShouldEqual(["/somewhere/Cratis.Cli.dll"]);
}
