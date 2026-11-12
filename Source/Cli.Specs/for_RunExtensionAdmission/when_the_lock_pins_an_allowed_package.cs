// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions;
using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Commands.Run.for_RunExtensionAdmission;

public class when_the_lock_pins_an_allowed_package : Specification
{
    ExtensionProject _project;
    IReadOnlyList<RunExtensionRefusal> _refusals;

    void Establish()
    {
        _project = new();
        _project.AllowPublisher();
        _project.Lock("2", ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2));
    }

    void Because()
    {
        var workspace = _project.Workspace();
        _refusals = RunExtensionAdmission.Check(workspace.References, workspace.Lock, ExtensionAllowList.Load(_project.Root));
    }

    void Destroy() => _project.Dispose();

    [Fact] void should_refuse_nothing() => _refusals.ShouldBeEmpty();
}
