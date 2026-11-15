// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_SystemdBuildService;

public class when_describing_a_service : Specification
{
    SystemdBuildService _systemd;
    BuildServiceDefinition _definition;
    string[] _lines;

    void Establish()
    {
        _systemd = new("/home/someone/.config");
        _definition = new("cratis-build-shop-12345678", "/usr/bin/cratis", ["build", "run", "--exec", "echo \"$HOME\" 100%"], "/src/My Shop 100%", "/usr/bin:/bin");
    }

    void Because() => _lines = _systemd.Render(_definition).Split('\n', StringSplitOptions.TrimEntries);

    [Fact] void should_be_a_user_unit() => _systemd.UnitPath(_definition.Name).ShouldEqual("/home/someone/.config/systemd/user/cratis-build-shop-12345678.service");
    [Fact] void should_quote_every_argument_and_keep_variables_and_specifiers_literal() =>
        _lines.ShouldContain("ExecStart=\"/usr/bin/cratis\" \"build\" \"run\" \"--exec\" \"echo \\\"$$HOME\\\" 100%%\"");
    [Fact] void should_run_in_the_project_written_unquoted() => _lines.ShouldContain("WorkingDirectory=/src/My Shop 100%%");
    [Fact] void should_describe_it_by_its_project() => _lines.ShouldContain("Description=Cratis builder for /src/My Shop 100%%");
    [Fact] void should_give_it_the_path() => _lines.ShouldContain("Environment=\"PATH=/usr/bin:/bin\"");
    [Fact] void should_restart_it_when_it_stops() => _lines.ShouldContain("Restart=always");
    [Fact] void should_start_it_with_the_user_s_session() => _lines.ShouldContain("WantedBy=default.target");
    [Fact] void should_read_its_output_from_the_journal() => _systemd.Logs(_definition.Name).ShouldEqual("journalctl --user -u cratis-build-shop-12345678.service");
}
