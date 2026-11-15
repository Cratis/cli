// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions;
using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Commands.Run.for_RunExtensionAdmission;

public class when_the_publisher_is_not_allowed : Specification
{
    ExtensionProject _project;
    IReadOnlyList<RunExtensionRefusal> _refusals;

    void Establish()
    {
        _project = new();
        _project.AllowPublisher("https://github.com/someone-else");
        _project.Lock("2", ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2));
    }

    void Because()
    {
        var workspace = _project.Workspace();
        _refusals = RunExtensionAdmission.Check(workspace.References, workspace.Lock, ExtensionAllowList.Load(_project.Root));
    }

    void Destroy() => _project.Dispose();

    [Fact] void should_refuse_the_reference() => _refusals.Single().Reference.ShouldEqual("cratis/stripe");
    [Fact] void should_say_it_is_unallowed() => _refusals.Single().Kind.ShouldEqual(ExtensionFailureKind.Unallowed);
    [Fact] void should_name_the_publisher() => _refusals.Single().Message.ShouldContain("https://github.com/cratis");
}
