// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions;
using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Commands.Run.for_RunExtensionAdmission;

public class when_the_publisher_may_not_supply_the_reference : Specification
{
    const string Publisher = "https://github.com/cratis";

    ExtensionProject _project;
    IReadOnlyList<RunExtensionRefusal> _refusals;

    void Establish()
    {
        _project = new();
        new ExtensionAllowList([]).Add(_project.Root, new(Publisher, ExtensionPackageFactory.FingerprintOf(Publisher), ["ghcr.io/somewhere-else/*"]));
        _project.Lock("2", ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2));
    }

    void Because()
    {
        var workspace = _project.Workspace();
        _refusals = RunExtensionAdmission.Check(workspace.References, workspace.Lock, ExtensionAllowList.Load(_project.Root));
    }

    void Destroy() => _project.Dispose();

    [Fact] void should_say_it_is_unallowed() => _refusals.Single().Kind.ShouldEqual(ExtensionFailureKind.Unallowed);
    [Fact] void should_name_the_reference_the_lock_resolved_to() => _refusals.Single().Message.ShouldContain(_project.Workspace().Lock.Entries.Single().Resolved);
}
