// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Cli.Registration;

namespace Cratis.Cli.for_CliApp;

public class when_requesting_screenplay_help : given.a_cli_process
{
    readonly Dictionary<string, (int ExitCode, string Output, string Error)> _results = new(StringComparer.Ordinal);

    async Task Because()
    {
        var commands = typeof(ValidateScreenplayCommand).Assembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributes<CliCommandAttribute>())
            .Where(command => command.Branch == typeof(ScreenplayBranch) || command.Branch?.DeclaringType == typeof(ScreenplayBranch));
        foreach (var command in commands)
        {
            string[] arguments = command.Branch == typeof(ScreenplayBranch)
                ? ["screenplay", command.Name, "--help"]
                : ["screenplay", "desktop", command.Name, "--help"];
            await Run(arguments);
            _results.Add(string.Join(' ', arguments), (_exitCode, _output, _error));
        }
    }

    [Fact] void should_exit_successfully_for_every_command() => _results.Where(result => result.Value.ExitCode != ExitCodes.Success).Select(result => result.Key).ShouldBeEmpty();
    [Fact] void should_render_options_for_every_command() => _results.Where(result => !result.Value.Output.Contains("OPTIONS:", StringComparison.Ordinal)).Select(result => result.Key).ShouldBeEmpty();
    [Fact] void should_not_report_markup_errors() => _results.Where(result => result.Value.Output.Contains("Error:", StringComparison.Ordinal) || result.Value.Error.Length > 0).Select(result => result.Key).ShouldBeEmpty();
    [Fact] void should_document_the_warning_alias() => _results["screenplay validate --help"].Output.ShouldContain("--warnaserror");
    [Fact] void should_render_the_scope_address_literally() => _results["screenplay validate --help"].Output.ShouldContain("Module[.Feature[.Slice]]");
    [Fact] void should_list_every_check_name() => new[] { "data-bindings", "input-surfaces", "field-origins", "query-keys", "event-consumers", "navigation", "personas", "purposes", "privilege", "enum-pinning", "all" }.All(name => _results["screenplay validate --help"].Output.Contains(name, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_document_exit_codes() => new[] { "Exit codes:", "0 for", "1 for", "5 for", "255 on POSIX" }.All(text => _results["screenplay validate --help"].Output.Contains(text, StringComparison.Ordinal)).ShouldBeTrue();
}
