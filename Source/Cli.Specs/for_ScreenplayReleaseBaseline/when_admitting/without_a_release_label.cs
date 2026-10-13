// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Cli.for_ScreenplayReleaseBaseline.when_admitting;

public class without_a_release_label : given.a_release_model
{
    Task Because() => Run("screenplay", "baseline", "admit", _root);
    [Fact] void should_reject_usage() => _exitCode.ShouldEqual(255);
    [Fact] void should_require_release() => _error.ShouldContain("--release is required");
    [Fact] void should_not_write_baseline() => File.Exists(Path.Combine(_root, ".screenplay", ReleaseBaselineStore.FileName)).ShouldBeFalse();
}
