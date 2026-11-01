// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateInstaller.when_ensuring_installed;

public class and_offline_with_a_cached_set : given.a_template_installer
{
    TemplateSelection _result;

    void Establish()
    {
        _source.Cache("1.6.0");
        _source.Cache("1.7.0");
        _source.Offline = true;
    }

    async Task Because() => _result = await _installer.EnsureInstalled();

    [Fact] void should_use_the_newest_cached_version() => _result.Version.ShouldEqual("1.7.0");
    [Fact] void should_report_it_as_cached() => _result.Origin.ShouldEqual(TemplateSelectionOrigin.Cached);
    [Fact] void should_say_why_the_latest_was_not_installed() => _result.Notice.ShouldContain("no such host");
    [Fact] void should_not_record_it_as_installed() => Installed.ShouldBeNull();
}
