// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.for_ExtensionService;

public class when_restoring_a_package_that_is_not_signed : Specification
{
    ExtensionProject _project;
    FixedPublisherPrompt _prompt;
    ExtensionReport _report;

    void Establish()
    {
        _project = new();
        _project.AllowPublisher();
        _prompt = new(true, true);
        _project.Registry.With(ExtensionPackageFactory.Unsigned("2.3.1", ExtensionContracts.Stripe2));
    }

    async Task Because() => _report = await _project.Service(_prompt).Restore(_project.Workspace(), false, CancellationToken.None);

    void Destroy() => _project.Dispose();

    [Fact] void should_fail_as_unsigned() => _report.Outcomes.Single().Failure.ShouldEqual(ExtensionFailureKind.NotSigned);
    [Fact] void should_not_offer_to_trust_it() => _prompt.Asked.ShouldEqual(0);
    [Fact] void should_not_write_the_lock() => _project.LockText.ShouldBeEmpty();
    [Fact] void should_not_fill_the_cache() => Directory.Exists(_project.Cache.Root).ShouldBeFalse();
}
