// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Reflection;
using Cratis.Cli.Commands.New;

namespace Cratis.Cli.for_NewSettings;

public class when_describing_the_language_option : Specification
{
    string _description = null!;

    void Because() => _description = typeof(NewSettings)
        .GetProperty(nameof(NewSettings.Language))!
        .GetCustomAttribute<DescriptionAttribute>()!
        .Description;

    [Fact] void should_name_csharp() => _description.ShouldContain("csharp (C#)");
    [Fact] void should_name_kotlin() => _description.ShouldContain("kotlin");
    [Fact] void should_name_java() => _description.ShouldContain("java");
    [Fact] void should_describe_the_hash_alias() => _description.ShouldContain("'c#' is accepted");
    [Fact] void should_describe_the_unnamed_default() => _description.ShouldContain("defaults to 'cratis' when no template is named");
    [Fact] void should_not_say_the_jvm_templates_are_future_work() => _description.ShouldNotContain("light up as their template packages ship");
}
