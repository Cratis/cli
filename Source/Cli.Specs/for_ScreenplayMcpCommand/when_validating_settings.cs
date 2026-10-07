// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayMcpCommand;

public class when_validating_settings : Specification
{
    [Fact] void should_accept_default_settings() => new ScreenplayMcpSettings().Validate().Successful.ShouldBeTrue();
    [Fact] void should_reject_path_and_project() => new ScreenplayMcpSettings { Path = "./models", ProjectRoot = "." }.Validate().Successful.ShouldBeFalse();
    [Fact] void should_reject_path_and_environment() => new ScreenplayMcpSettings { Path = "./models", ProjectRootEnvironment = "PROJECT" }.Validate().Successful.ShouldBeFalse();
    [Fact] void should_reject_project_and_environment() => new ScreenplayMcpSettings { ProjectRoot = ".", ProjectRootEnvironment = "PROJECT" }.Validate().Successful.ShouldBeFalse();
    [Fact] void should_reject_reserved_desktop_names()
    {
        foreach (var verb in new[] { "install", "status", "update", "uninstall" })
        {
            var result = new ScreenplayMcpSettings { Path = verb }.Validate();
            result.Successful.ShouldBeFalse();
            result.Message.ShouldContain($"cratis screenplay desktop {verb}");
            result.Message.ShouldContain($"./{verb}");
        }
    }
    [Fact] void should_accept_explicit_reserved_directory() => new ScreenplayMcpSettings { Path = "./install" }.Validate().Successful.ShouldBeTrue();
}
