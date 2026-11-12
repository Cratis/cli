// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run.for_RunExtensions;

using Cratis.Cli.Extensions;

public class when_everything_is_pinned_allowed_and_cached : given.an_admission
{
    void Establish()
    {
        Restore();
        var key = Convert.ToBase64String(ExtensionPackageFactory.PublicKeyOf("https://github.com/cratis"));
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(Project.Cache.PathOf(Pinned.Digest))!, ExtensionService.KeyFileName), key);
    }

    Task Because() => Admit(Project.Model, true);

    [Fact] void should_let_the_run_start() => Admitted.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_hold_the_workspace() => Admitted.Workspace.References.Single().Reference.ShouldEqual("cratis/stripe");
    [Fact] void should_hand_over_the_key_of_the_publisher() => Admitted.Keys["https://github.com/cratis"].Single().ShouldEqual(Convert.ToBase64String(ExtensionPackageFactory.PublicKeyOf("https://github.com/cratis")));
    [Fact] void should_print_nothing() => Output.ShouldBeEmpty();
}
