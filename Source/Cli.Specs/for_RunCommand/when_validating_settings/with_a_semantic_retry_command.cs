// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.when_validating_settings;

public class with_a_semantic_retry_command : Specification
{
    string _defaults = null!;
    string _custom = null!;
    string _quoted = null!;

    void Because()
    {
        _defaults = new RunSettings { Path = "./models" }.SemanticCommand();
        _custom = new RunSettings
        {
            Path = "./my models",
            Port = 9191,
            WorkbenchPort = 35001,
            Tag = "1.2.3",
            PackageSet = "./approved set.json",
            Packages = "./node_modules/@acme",
            EnvironmentShort = "sandbox",
            AuthPort = 8181,
            Verbose = true,
            AllowRemote = true
        }.SemanticCommand();
        _quoted = new RunSettings { Path = string.Empty, Tag = "a\"$`\\b" }.SemanticCommand();
    }

    [Fact]
    void should_omit_every_default_option() => _defaults.ShouldEqual("cratis run ./models --engine semantic");

    [Fact]
    void should_preserve_non_default_options_in_order_and_quote_spaces() => _custom.ShouldEqual(
        OperatingSystem.IsWindows()
            ? "cratis run \"./my models\" --engine semantic --port 9191 --workbench-port 35001 --tag 1.2.3 --package-set \"./approved set.json\" --packages ./node_modules/@acme -e sandbox --auth-port 8181 --verbose --allow-remote"
            : "cratis run './my models' --engine semantic --port 9191 --workbench-port 35001 --tag 1.2.3 --package-set './approved set.json' --packages ./node_modules/@acme -e sandbox --auth-port 8181 --verbose --allow-remote");

    [Fact]
    void should_quote_an_empty_path_and_escape_shell_special_characters() => _quoted.ShouldEqual(
        OperatingSystem.IsWindows()
            ? "cratis run \"\" --engine semantic --tag \"a`\"`$``\\b\""
            : "cratis run '' --engine semantic --tag 'a\"$`\\b'");
}
