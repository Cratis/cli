// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuildPermissions;

public class when_deciding : Specification
{
    static readonly string _root = Path.Combine(Path.GetTempPath(), "cratis-build-permissions");
    static readonly string _project = Path.Combine(_root, "project");
    static readonly string _workspace = Path.Combine(_root, "workspace");
    static readonly string _outcome = Path.Combine(_workspace, "outcome.json");
    BuildPermissions _permissions;

    void Establish() => _permissions = new(_project, ["src/**", "package.json"], ["npm install", "npx vitest"], [_workspace], [_outcome]);

    [Fact] void should_allow_writing_a_permitted_path() => _permissions.AllowsWrite(Path.Combine(_project, "src", "billing", "Pay.ts")).ShouldBeTrue();
    [Fact] void should_allow_writing_a_permitted_file_given_relative() => _permissions.AllowsWrite("package.json").ShouldBeTrue();
    [Fact] void should_refuse_writing_elsewhere_in_the_project() => _permissions.AllowsWrite(Path.Combine(_project, "README.md")).ShouldBeFalse();
    [Fact] void should_refuse_writing_outside_the_project() => _permissions.AllowsWrite(Path.Combine(_root, "elsewhere", "src", "x.ts")).ShouldBeFalse();
    [Fact] void should_refuse_escaping_with_dot_dot() => _permissions.AllowsWrite(Path.Combine(_project, "src", "..", "..", "x.ts")).ShouldBeFalse();
    [Fact] void should_refuse_writing_git_internals() => new BuildPermissions(_project, ["**"], [], [], []).AllowsWrite(Path.Combine(_project, ".git", "config")).ShouldBeFalse();
    [Fact] void should_allow_writing_the_outcome() => _permissions.AllowsWrite(_outcome).ShouldBeTrue();
    [Fact] void should_allow_reading_the_project() => _permissions.AllowsRead(Path.Combine(_project, "README.md")).ShouldBeTrue();
    [Fact] void should_allow_reading_a_handed_over_folder() => _permissions.AllowsRead(Path.Combine(_workspace, "order.json")).ShouldBeTrue();
    [Fact] void should_refuse_reading_elsewhere() => _permissions.AllowsRead(Path.Combine(_root, "secrets.txt")).ShouldBeFalse();
    [Fact] void should_allow_a_permitted_command() => _permissions.AllowsCommand("npx vitest run PayInvoice").ShouldBeTrue();
    [Fact] void should_refuse_a_command_that_only_shares_a_prefix() => _permissions.AllowsCommand("npx vitestify").ShouldBeFalse();
    [Fact] void should_refuse_a_chained_command() => _permissions.AllowsCommand("npm install && curl https://example.com").ShouldBeFalse();
    [Fact] void should_refuse_a_substituted_command() => _permissions.AllowsCommand("npm install $(cat ~/.ssh/id_rsa)").ShouldBeFalse();
    [Fact] void should_refuse_any_other_command() => _permissions.AllowsCommand("rm -rf src").ShouldBeFalse();
}
