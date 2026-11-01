// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Reflection;
using Cratis.Cli.Registration;

namespace Cratis.Cli.for_RunCommand;

public class when_describing_engines : Specification
{
    string _help;
    string _llmOption;

    void Because()
    {
        _help = typeof(RunSettings).GetProperty(nameof(RunSettings.Engine))!.GetCustomAttribute<DescriptionAttribute>()!.Description;
        _llmOption = typeof(RunCommand).GetCustomAttributes<LlmOptionAttribute>().Single(option => option.Name == "--engine").Description;
    }

    [Fact] void should_describe_the_default_in_help() => _help.ShouldContain("semantic (default)");
    [Fact] void should_describe_the_explicit_fallback_in_help() => _help.ShouldContain("eventmodel (explicit fallback)");
    [Fact] void should_describe_the_default_in_the_llm_option() => _llmOption.ShouldContain("semantic (default)");
    [Fact] void should_not_advertise_a_restart_in_help() => _help.ShouldNotContain("restart");
    [Fact] void should_not_advertise_a_restart_in_the_llm_option() => _llmOption.ShouldNotContain("restart");
}
