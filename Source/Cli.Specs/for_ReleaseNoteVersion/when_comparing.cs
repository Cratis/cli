// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.ReleaseNotes;

namespace Cratis.Cli.for_ReleaseNoteVersion;

public class when_comparing : Specification
{
    [Fact] void should_compare_numerically() => (ReleaseNoteVersion.Compare("3.10.0", "3.9.0") > 0).ShouldBeTrue();
    [Fact] void should_ignore_a_leading_v_and_build_metadata() => ReleaseNoteVersion.Compare("v3.9.0", "3.9.0+abc").ShouldEqual(0);
    [Fact] void should_put_a_prerelease_below_its_release() => (ReleaseNoteVersion.Compare("3.9.0-beta.1", "3.9.0") < 0).ShouldBeTrue();
    [Fact] void should_put_a_release_above_its_prerelease() => (ReleaseNoteVersion.Compare("3.9.0", "3.9.0-beta.1") > 0).ShouldBeTrue();
}
