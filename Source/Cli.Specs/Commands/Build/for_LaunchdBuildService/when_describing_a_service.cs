// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace Cratis.Cli.Commands.Build.for_LaunchdBuildService;

public class when_describing_a_service : Specification
{
    LaunchdBuildService _launchd;
    BuildServiceDefinition _definition;
    XDocument _plist;
    string[] _strings;

    void Establish()
    {
        _launchd = new("/Users/someone", "501");
        _definition = new("cratis-build-shop-12345678", "/usr/local/bin/cratis", ["build", "run", "--project", "/src/R&D <shop>", "--exec", "./gen.sh \"a\""], "/src/R&D <shop>", "/opt/homebrew/bin:/usr/bin");
    }

    void Because()
    {
        _plist = XDocument.Parse(_launchd.Render(_definition), LoadOptions.None);
        _strings = [.. _plist.Descendants("array").Single().Elements("string").Select(element => element.Value)];
    }

    [Fact] void should_be_kept_in_the_user_s_launch_agents() =>
        _launchd.UnitPath(_definition.Name).ShouldEqual("/Users/someone/Library/LaunchAgents/io.cratis.cratis-build-shop-12345678.plist");
    [Fact] void should_run_the_program_with_its_arguments_unchanged() =>
        _strings.ShouldEqual(["/usr/local/bin/cratis", "build", "run", "--project", "/src/R&D <shop>", "--exec", "./gen.sh \"a\""]);
    [Fact] void should_label_it() => _plist.Descendants("string").First().Value.ShouldEqual("io.cratis.cratis-build-shop-12345678");
    [Fact] void should_run_at_load() => Following("RunAtLoad").Name.LocalName.ShouldEqual("true");
    [Fact] void should_keep_it_alive() => Following("KeepAlive").Name.LocalName.ShouldEqual("true");
    [Fact] void should_run_in_the_project() => Following("WorkingDirectory").Value.ShouldEqual("/src/R&D <shop>");
    [Fact] void should_give_it_the_path() => Following("EnvironmentVariables").Value.ShouldEqual("PATH/opt/homebrew/bin:/usr/bin");
    [Fact] void should_write_its_output_to_the_user_s_logs() =>
        Following("StandardOutPath").Value.ShouldEqual("/Users/someone/Library/Logs/Cratis/cratis-build-shop-12345678.log");

    XElement Following(string key) =>
        _plist.Root!.Element("dict")!.Elements("key").Single(element => element.Value == key).ElementsAfterSelf().First();
}
