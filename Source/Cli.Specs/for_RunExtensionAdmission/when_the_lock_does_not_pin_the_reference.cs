// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions;
using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Commands.Run.for_RunExtensionAdmission;

public class when_the_lock_does_not_pin_the_reference : Specification
{
    ExtensionProject _project;
    IReadOnlyList<RunExtensionRefusal> _refusals;

    void Establish()
    {
        _project = new();
        _project.AllowPublisher();
    }

    void Because()
    {
        var workspace = _project.Workspace();
        _refusals = RunExtensionAdmission.Check(workspace.References, workspace.Lock, ExtensionAllowList.Load(_project.Root));
    }

    void Destroy() => _project.Dispose();

    [Fact] void should_refuse_the_reference() => _refusals.Single().Reference.ShouldEqual("cratis/stripe");
    [Fact] void should_say_it_is_not_locked() => _refusals.Single().Kind.ShouldEqual(ExtensionFailureKind.NotLocked);
    [Fact] void should_point_at_restoring_it() => _refusals.Single().Message.ShouldContain("cratis extension restore");
}
