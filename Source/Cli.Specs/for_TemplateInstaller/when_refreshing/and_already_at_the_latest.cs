// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateInstaller.when_refreshing;

public class and_already_at_the_latest : given.a_template_installer
{
    TemplateRefreshResult _result;

    void Establish()
    {
        Install("1.8.0");
        _source.Publish("1.8.0");
    }

    async Task Because() => _result = await _installer.Refresh();

    [Fact] void should_report_no_update() => _result.ShouldEqual(new TemplateRefreshResult("1.8.0", "1.8.0", false));
    [Fact] void should_download_nothing() => _source.Acquired.ShouldBeEmpty();
}
