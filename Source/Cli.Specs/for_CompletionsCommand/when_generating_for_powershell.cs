// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CompletionsCommand;

public class when_generating_for_powershell : Specification
{
    string _result;

    void Because() => _result = PowerShellCompletionGenerator.Generate();

    [Fact] void should_contain_register_argument_completer() => _result.ShouldContain("Register-ArgumentCompleter");
    [Fact] void should_contain_native_flag() => _result.ShouldContain("-Native");
    [Fact] void should_contain_cratis_script_block() => _result.ShouldContain("'cratis'");
    [Fact] void should_contain_path_switch() => _result.ShouldContain("switch ($path)");
}
