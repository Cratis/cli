// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.for_ExtensionService;

public class when_restoring_from_a_publisher_the_project_does_not_allow : Specification
{
    ExtensionProject _noPrompt;
    ExtensionProject _refused;
    ExtensionProject _accepted;
    FixedPublisherPrompt _refusing;
    FixedPublisherPrompt _accepting;
    FixedPublisherPrompt _offline;
    ExtensionReport _nonInteractive;
    ExtensionReport _declined;
    ExtensionReport _confirmed;

    static ExtensionProject Project()
    {
        var project = new ExtensionProject();
        project.Registry.With(ExtensionPackageFactory.Signed("2.3.1", ExtensionContracts.Stripe2));
        return project;
    }

    void Establish()
    {
        _noPrompt = Project();
        _refused = Project();
        _accepted = Project();
        _offline = new(false, true);
        _refusing = new(true, false);
        _accepting = new(true, true);
    }

    async Task Because()
    {
        _nonInteractive = await _noPrompt.Service(_offline).Restore(_noPrompt.Workspace(), false, CancellationToken.None);
        _declined = await _refused.Service(_refusing).Restore(_refused.Workspace(), false, CancellationToken.None);
        _confirmed = await _accepted.Service(_accepting).Restore(_accepted.Workspace(), false, CancellationToken.None);
    }

    void Destroy()
    {
        _noPrompt.Dispose();
        _refused.Dispose();
        _accepted.Dispose();
    }

    [Fact] void should_fail_a_non_interactive_run_as_unallowed() => _nonInteractive.Outcomes.Single().Failure.ShouldEqual(ExtensionFailureKind.Unallowed);
    [Fact] void should_not_ask_when_it_cannot() => _offline.Asked.ShouldEqual(0);
    [Fact] void should_not_write_the_lock_when_unallowed() => _noPrompt.LockText.ShouldBeEmpty();
    [Fact] void should_ask_a_person_about_a_first_time_publisher() => _refusing.Asked.ShouldEqual(1);
    [Fact] void should_fail_when_the_person_declines() => _declined.Outcomes.Single().Failure.ShouldEqual(ExtensionFailureKind.Unallowed);
    [Fact] void should_succeed_when_the_person_accepts() => _confirmed.Succeeded.ShouldBeTrue();
    [Fact] void should_record_the_trusted_publisher_in_the_allow_list() => ExtensionAllowList.Load(_accepted.Root).Find("https://github.com/cratis")!.Key.ShouldEqual(ExtensionPackageFactory.FingerprintOf("https://github.com/cratis"));
    [Fact] void should_not_record_a_declined_publisher() => ExtensionAllowList.Load(_refused.Root).Publishers.ShouldBeEmpty();
}
