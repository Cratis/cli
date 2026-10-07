// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_validating_versions : Specification
{
    [Fact] void should_accept_an_omitted_version() => new DesktopMcpSettings().Validate().Successful.ShouldBeTrue();
    [Fact] void should_accept_a_semantic_release_version() => new DesktopMcpSettings { Version = "4.55.0" }.Validate().Successful.ShouldBeTrue();
    [Fact] void should_reject_a_leading_v() => new DesktopMcpSettings { Version = "v4.55.0" }.Validate().Successful.ShouldBeFalse();
    [Fact] void should_preserve_the_version_guidance() => new DesktopMcpSettings { Version = "invalid" }.Validate().Message.ShouldContain("Expected a Screenplay semantic version");
}
