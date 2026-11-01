// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateInstaller.when_refreshing;

public class and_a_newer_set_is_published : given.a_template_installer
{
    TemplateRefreshResult _result;

    void Establish()
    {
        Install("1.7.0");
        _source.Publish("1.8.0");
    }

    async Task Because() => _result = await _installer.Refresh();

    [Fact] void should_report_the_update() => _result.ShouldEqual(new TemplateRefreshResult("1.7.0", "1.8.0", true));
    [Fact] void should_record_the_newer_set_as_installed() => Installed!.Version.ShouldEqual("1.8.0");
}
