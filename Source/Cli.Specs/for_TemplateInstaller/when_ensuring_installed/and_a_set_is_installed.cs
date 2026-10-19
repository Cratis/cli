// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateInstaller.when_ensuring_installed;

public class and_a_set_is_installed : given.a_template_installer
{
    TemplateSelection _result;

    void Establish()
    {
        Install("1.7.0");
        _source.Publish("1.8.0");
    }

    async Task Because() => _result = await _installer.EnsureInstalled();

    [Fact] void should_use_the_installed_version() => _result.Version.ShouldEqual("1.7.0");
    [Fact] void should_report_it_as_installed() => _result.Origin.ShouldEqual(TemplateSelectionOrigin.Installed);
    [Fact] void should_not_ask_the_feeds_for_a_newer_one() => _source.LatestVersionRequests.ShouldEqual(0);
}
