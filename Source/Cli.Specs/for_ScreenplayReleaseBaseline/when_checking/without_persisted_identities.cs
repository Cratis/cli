// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayReleaseBaseline.when_checking;

public class without_persisted_identities : given.a_release_model
{
    void Establish() => File.Delete(Path.Combine(_root, ".screenplay", "identities.json"));
    Task Because() => Run("screenplay", "baseline", "check", _root);
    [Fact] void should_refuse() => _exitCode.ShouldEqual(2);
    [Fact] void should_explain_the_missing_state() => _error.ShouldContain("PersistedIdentitiesRequired:");
}
