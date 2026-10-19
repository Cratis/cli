// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateInstaller.when_refreshing;

public class and_nothing_is_installed : given.a_template_installer
{
    TemplateRefreshResult _result;

    void Establish() => _source.Publish("1.8.0");

    async Task Because() => _result = await _installer.Refresh();

    [Fact] void should_report_the_installation() => _result.ShouldEqual(new TemplateRefreshResult(null, "1.8.0", true));
    [Fact] void should_record_the_set_as_installed() => Installed!.Version.ShouldEqual("1.8.0");
}
