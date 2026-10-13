// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Cli.for_ScreenplayReleaseBaseline.when_admitting;

public class with_a_release_label : given.a_release_model
{
    Task Because() => Run("screenplay", "baseline", "admit", _root, "--release", "v1");
    [Fact] void should_admit() => _exitCode.ShouldEqual(0);
    [Fact] void should_print_count_and_revision() => _output.ShouldContain($"Admitted 1 event revision(s) in release baseline {ReleaseBaselineStore.Revision(File.ReadAllBytes(Path.Combine(_root, ".screenplay", ReleaseBaselineStore.FileName)))}.");
}
